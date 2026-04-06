#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Unityctl.Plugin.Editor.Commands
{
    internal static class UitkElementResolver
    {
        internal sealed class ResolvedElement
        {
            public object Element { get; set; }
            public Component DocumentComponent { get; set; }
            public string DocumentName { get; set; }
            public string ElementPath { get; set; }
            public string Locator { get; set; }
            public string Name { get; set; }
            public string TypeName { get; set; }
            public string FullTypeName { get; set; }
        }

        public static Type FindUidocumentType()
        {
            return FindType("UnityEngine.UIElements.UIDocument");
        }

        public static IReadOnlyList<ResolvedElement> Find(string nameFilter, string classNameFilter, string typeFilter, int limit)
        {
            var uiDocType = FindUidocumentType();
            if (uiDocType == null)
                return Array.Empty<ResolvedElement>();

            var results = new List<ResolvedElement>();
            var docs = UnityEngine.Object.FindObjectsByType(uiDocType, FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var doc in docs)
            {
                var component = doc as Component;
                if (component == null)
                    continue;

                var root = GetRootVisualElement(doc);
                if (root == null)
                    continue;

                Traverse(component, root, "root", nameFilter, classNameFilter, typeFilter, results, limit);
                if (limit > 0 && results.Count >= limit)
                    break;
            }

            return results;
        }

        public static bool TryResolveSingle(string name, string locator, out ResolvedElement resolved, out JArray candidates, out bool ambiguous)
        {
            resolved = null;
            candidates = new JArray();
            ambiguous = false;

            var all = Find(null, null, null, 0);
            IEnumerable<ResolvedElement> matches;

            if (!string.IsNullOrWhiteSpace(locator))
            {
                matches = all.Where(item => string.Equals(item.Locator, locator, StringComparison.OrdinalIgnoreCase));
            }
            else if (!string.IsNullOrWhiteSpace(name))
            {
                var exactMatches = all.Where(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)).ToArray();
                matches = exactMatches.Length > 0
                    ? exactMatches
                    : all.Where(item => !string.IsNullOrWhiteSpace(item.Name)
                                        && item.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            else
            {
                return false;
            }

            var materialized = matches.ToArray();
            if (materialized.Length == 0)
                return false;

            if (materialized.Length > 1)
            {
                ambiguous = true;
                foreach (var item in materialized.Take(10))
                    candidates.Add(ToSummary(item));
                return false;
            }

            resolved = materialized[0];
            return true;
        }

        public static JObject ToSummary(ResolvedElement item)
        {
            return new JObject
            {
                ["name"] = item.Name ?? string.Empty,
                ["type"] = item.TypeName,
                ["fullType"] = item.FullTypeName,
                ["documentName"] = item.DocumentName,
                ["elementPath"] = item.ElementPath,
                ["locator"] = item.Locator,
                ["visible"] = GetBoolProp(item.Element, "visible"),
                ["enabledSelf"] = GetBoolProp(item.Element, "enabledSelf")
            };
        }

        private static void Traverse(Component documentComponent, object element, string path, string nameFilter, string classNameFilter,
            string typeFilter, List<ResolvedElement> results, int limit)
        {
            if (limit > 0 && results.Count >= limit)
                return;

            var current = BuildResolvedElement(documentComponent, element, path);
            if (Matches(current, classNameFilter, typeFilter, nameFilter))
                results.Add(current);

            var children = GetChildren(element);
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child == null)
                    continue;

                var childType = child.GetType();
                var childName = GetStringProp(child, "name");
                var pathSegment = $"{i}:{childType.Name}";
                if (!string.IsNullOrWhiteSpace(childName))
                    pathSegment += $"#{childName}";
                Traverse(documentComponent, child, path + "/" + pathSegment, nameFilter, classNameFilter, typeFilter, results, limit);
                if (limit > 0 && results.Count >= limit)
                    return;
            }
        }

        private static ResolvedElement BuildResolvedElement(Component documentComponent, object element, string elementPath)
        {
            var elementType = element.GetType();
            var documentName = documentComponent.gameObject.name;
            return new ResolvedElement
            {
                Element = element,
                DocumentComponent = documentComponent,
                DocumentName = documentName,
                ElementPath = elementPath,
                Locator = $"{documentName}::{elementPath}",
                Name = GetStringProp(element, "name") ?? string.Empty,
                TypeName = elementType.Name,
                FullTypeName = elementType.FullName
            };
        }

        private static bool Matches(ResolvedElement current, string classNameFilter, string typeFilter, string nameFilter)
        {
            if (!string.IsNullOrWhiteSpace(nameFilter)
                && (string.IsNullOrWhiteSpace(current.Name)
                    || current.Name.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) < 0))
                return false;

            if (!string.IsNullOrWhiteSpace(typeFilter)
                && !string.Equals(current.TypeName, typeFilter, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.IsNullOrWhiteSpace(classNameFilter))
            {
                var classes = GetClassList(current.Element);
                if (classes == null || !ContainsClass(classes, classNameFilter))
                    return false;
            }

            return true;
        }

        private static object GetRootVisualElement(object document)
        {
            var rootProp = document.GetType().GetProperty("rootVisualElement");
            return rootProp?.GetValue(document);
        }

        private static List<object> GetChildren(object element)
        {
            var children = new List<object>();
            var elementType = element.GetType();
            var childCountProp = elementType.GetProperty("childCount");
            if (childCountProp == null)
                return children;

            var childCount = (int)childCountProp.GetValue(element);
            var indexer = elementType.GetMethod("ElementAt") ?? elementType.GetMethod("get_Item");
            if (indexer == null)
                return children;

            for (var i = 0; i < childCount; i++)
            {
                var child = indexer.Invoke(element, new object[] { i });
                if (child != null)
                    children.Add(child);
            }

            return children;
        }

        private static string GetStringProp(object obj, string name)
        {
            var prop = obj.GetType().GetProperty(name);
            return prop?.GetValue(obj) as string;
        }

        private static bool GetBoolProp(object obj, string name)
        {
            var prop = obj.GetType().GetProperty(name);
            if (prop != null && prop.PropertyType == typeof(bool))
                return (bool)prop.GetValue(obj);
            return true;
        }

        private static IEnumerable GetClassList(object element)
        {
            var method = element.GetType().GetMethod("GetClasses");
            return method?.Invoke(element, null) as IEnumerable;
        }

        private static bool ContainsClass(IEnumerable classList, string className)
        {
            foreach (var cls in classList)
            {
                if (cls is string s && s.Equals(className, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static Type FindType(string typeName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType(typeName);
                if (type != null)
                    return type;
            }

            return null;
        }
    }
}
#endif
