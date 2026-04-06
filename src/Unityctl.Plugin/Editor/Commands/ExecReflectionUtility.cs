#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    internal static class ExecReflectionUtility
    {
        private static readonly string[] BlockedTypePatterns =
        {
            "System.IO.File",
            "System.IO.Directory",
            "System.IO.Path",
            "System.Diagnostics.Process",
            "System.Net.",
            "System.Reflection.Emit",
            "System.Runtime.InteropServices.Marshal"
        };

        public static object EvaluateExpression(string expr)
        {
            ExecExpression parsed;
            try
            {
                parsed = ExecExpressionParser.Parse(expr);
            }
            catch (ExecExpressionParseException ex)
            {
                throw new ExecParseException(ex.Message);
            }

            return parsed.Kind switch
            {
                ExecExpressionKind.GetMember => GetMember(parsed.TypeName, parsed.MemberName),
                ExecExpressionKind.SetMember => SetMember(parsed.TypeName, parsed.MemberName, parsed.RightHandSide),
                ExecExpressionKind.InvokeMethod => InvokeMethod(parsed.TypeName, parsed.MemberName, parsed.Arguments),
                _ => throw new ExecParseException("Unsupported exec expression.")
            };
        }

        public static JArray ListCallables(string filter, int limit)
        {
            var normalizedFilter = string.IsNullOrWhiteSpace(filter)
                ? null
                : filter.Trim();
            var results = new JArray();

            foreach (var type in EnumerateCallableTypes())
            {
                if (!MatchesFilter(type, normalizedFilter))
                    continue;

                var methods = type
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(method => !method.IsSpecialName)
                    .Select(method => method.Name)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToArray();

                var properties = type
                    .GetProperties(BindingFlags.Public | BindingFlags.Static)
                    .Select(property => property.Name)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToArray();

                if (methods.Length == 0 && properties.Length == 0)
                    continue;

                results.Add(new JObject
                {
                    ["typeName"] = type.FullName,
                    ["assemblyName"] = type.Assembly.GetName().Name,
                    ["methods"] = new JArray(methods),
                    ["properties"] = new JArray(properties)
                });

                if (limit > 0 && results.Count >= limit)
                    break;
            }

            return results;
        }

        public static object InvokeStructured(string typeName, string methodName, string argsJson)
        {
            var rawArgs = ParseArguments(argsJson);
            return InvokeMethod(typeName, methodName, rawArgs.Select(token => token.ToString(Newtonsoft.Json.Formatting.None)).ToArray());
        }

        private static object GetMember(string typeName, string memberName)
        {
            var type = ResolveType(typeName)
                ?? throw new ExecParseException($"Type not found: '{typeName}'. Ensure the type is loaded in the current AppDomain.");
            try
            {
                var prop = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Static);
                if (prop != null)
                    return prop.GetValue(null);

                var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Static);
                if (field != null)
                    return field.GetValue(null);
            }
            catch (TargetInvocationException tie)
            {
                throw ExecInvocationException.Create(type.FullName, memberName, tie);
            }

            throw new ExecParseException($"No public static property or field '{memberName}' on {type.FullName}.");
        }

        private static object SetMember(string typeName, string memberName, string rhs)
        {
            var type = ResolveType(typeName)
                ?? throw new ExecParseException($"Type not found: '{typeName}'. Ensure the type is loaded in the current AppDomain.");

            var prop = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Static);
            if (prop != null)
            {
                var value = ConvertArgumentString(rhs, prop.PropertyType);
                try
                {
                    prop.SetValue(null, value);
                    return value;
                }
                catch (TargetInvocationException tie)
                {
                    throw ExecInvocationException.Create(type.FullName, memberName, tie);
                }
            }

            var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Static);
            if (field != null)
            {
                var value = ConvertArgumentString(rhs, field.FieldType);
                try
                {
                    field.SetValue(null, value);
                    return value;
                }
                catch (TargetInvocationException tie)
                {
                    throw ExecInvocationException.Create(type.FullName, memberName, tie);
                }
            }

            throw new ExecParseException($"No settable public static property or field '{memberName}' on {type.FullName}.");
        }

        private static object InvokeMethod(string typeName, string methodName, IReadOnlyList<string> rawArguments)
        {
            var type = ResolveType(typeName)
                ?? throw new ExecParseException($"Type not found: '{typeName}'. Ensure the type is loaded in the current AppDomain.");

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => string.Equals(method.Name, methodName, StringComparison.Ordinal))
                .ToArray();
            if (methods.Length == 0)
                throw new ExecParseException($"No public static method '{methodName}' on {type.FullName}.");

            var match = methods.FirstOrDefault(method => method.GetParameters().Length == rawArguments.Count)
                ?? methods[0];
            var parameters = match.GetParameters();
            if (parameters.Length != rawArguments.Count)
            {
                throw new ExecParseException(
                    $"Method '{methodName}' on {type.FullName} expects {parameters.Length} argument(s), but received {rawArguments.Count}.");
            }

            var convertedArgs = new object[rawArguments.Count];
            for (var i = 0; i < rawArguments.Count; i++)
                convertedArgs[i] = ConvertArgumentString(rawArguments[i], parameters[i].ParameterType);

            try
            {
                return match.Invoke(null, convertedArgs);
            }
            catch (TargetInvocationException tie)
            {
                throw ExecInvocationException.Create(type.FullName, methodName, tie);
            }
        }

        internal static Type ResolveType(string typeName)
        {
            foreach (var blocked in BlockedTypePatterns)
            {
                if (typeName.StartsWith(blocked, StringComparison.OrdinalIgnoreCase))
                    throw new ExecSecurityException($"Type '{typeName}' is blocked for safety.");
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                var exact = asm.GetType(typeName, false, false);
                if (exact != null)
                    return exact;
            }

            var shortMatches = assemblies
                .Where(assembly => !assembly.IsDynamic)
                .SelectMany(SafeGetTypes)
                .Where(type => type != null
                               && string.Equals(type.Name, typeName, StringComparison.Ordinal)
                               && IsCallableType(type))
                .Distinct()
                .ToArray();

            return shortMatches.Length == 1 ? shortMatches[0] : null;
        }

        private static IEnumerable<Type> EnumerateCallableTypes()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !assembly.IsDynamic)
                .OrderBy(assembly => assembly.GetName().Name, StringComparer.Ordinal);

            foreach (var assembly in assemblies)
            {
                foreach (var type in SafeGetTypes(assembly))
                {
                    if (type == null || !IsCallableType(type))
                        continue;

                    yield return type;
                }
            }
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null);
            }
        }

        private static bool IsCallableType(Type type)
        {
            return !string.IsNullOrWhiteSpace(type.FullName)
                   && !type.IsGenericTypeDefinition
                   && !type.IsNestedPrivate
                   && (type.GetMethods(BindingFlags.Public | BindingFlags.Static).Any(method => !method.IsSpecialName)
                       || type.GetProperties(BindingFlags.Public | BindingFlags.Static).Length > 0);
        }

        private static bool MatchesFilter(Type type, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            return type.FullName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                   || type.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                   || type.Assembly.GetName().Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static JToken[] ParseArguments(string argsJson)
        {
            if (string.IsNullOrWhiteSpace(argsJson))
                return Array.Empty<JToken>();

            var token = JToken.Parse(argsJson);
            if (token is not JArray array)
                throw new ExecParseException("Parameter 'args' must be a JSON array.");

            return array.ToArray();
        }

        private static object ConvertArgumentString(string raw, Type targetType)
        {
            var token = ParseToken(raw);
            return ConvertToken(token, targetType);
        }

        private static JToken ParseToken(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return JValue.CreateString(string.Empty);

            var trimmed = raw.Trim();
            if (trimmed[0] == '\'' && trimmed[trimmed.Length - 1] == '\'')
                return new JValue(trimmed.Substring(1, trimmed.Length - 2));

            if (trimmed[0] == '"' || trimmed[0] == '[' || trimmed[0] == '{'
                || string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmed, "null", StringComparison.OrdinalIgnoreCase)
                || char.IsDigit(trimmed[0]) || trimmed[0] == '-')
            {
                try
                {
                    return JToken.Parse(trimmed);
                }
                catch
                {
                    // Fall back to raw string conversion below.
                }
            }

            return new JValue(trimmed);
        }

        private static object ConvertToken(JToken token, Type targetType)
        {
            if (token.Type == JTokenType.Null)
                return null;

            if (targetType == typeof(string))
                return token.Type == JTokenType.String ? token.Value<string>() : token.ToString(Newtonsoft.Json.Formatting.None);

            if (targetType == typeof(bool))
                return token.Value<bool>();

            if (targetType == typeof(int))
                return token.Value<int>();

            if (targetType == typeof(float))
                return token.Value<float>();

            if (targetType == typeof(double))
                return token.Value<double>();

            if (targetType == typeof(long))
                return token.Value<long>();

            if (targetType.IsEnum)
                return Enum.Parse(targetType, token.Value<string>() ?? string.Empty, true);

            if (targetType == typeof(object))
                return token.ToObject<object>();

            return token.ToObject(targetType);
        }
    }

    internal sealed class ExecSecurityException : Exception
    {
        public ExecSecurityException(string message) : base(message) { }
    }

    internal sealed class ExecParseException : Exception
    {
        public ExecParseException(string message) : base(message) { }
    }

    internal sealed class ExecInvocationException : Exception
    {
        private ExecInvocationException(string typeName, string memberName, Exception inner)
            : base(inner.Message, inner)
        {
            TypeName = typeName;
            MemberName = memberName;
            InnerTypeName = inner.GetType().FullName ?? inner.GetType().Name;
            InnerStackTrace = inner.StackTrace;
        }

        public string TypeName { get; }
        public string MemberName { get; }
        public string InnerTypeName { get; }
        public string InnerStackTrace { get; }

        public static ExecInvocationException Create(string typeName, string memberName, TargetInvocationException tie)
        {
            return new ExecInvocationException(typeName, memberName, tie.InnerException ?? tie);
        }
    }
}
#endif
