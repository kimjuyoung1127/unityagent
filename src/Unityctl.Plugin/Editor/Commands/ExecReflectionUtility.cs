#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

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

        public static object? EvaluateExpression(string expr)
        {
            var eqIdx = expr.IndexOf('=');
            if (eqIdx > 0 && !expr.Contains("=="))
            {
                var lhs = expr.Substring(0, eqIdx).Trim();
                var rhs = expr.Substring(eqIdx + 1).Trim();
                return SetMember(lhs, rhs);
            }

            if (expr.Contains('('))
                return InvokeExpression(expr);

            return GetMember(expr);
        }

        public static JArray ListCallables(string? filter, int limit)
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

        public static object? InvokeStructured(string typeName, string methodName, string? argsJson)
        {
            var type = ResolveType(typeName)
                ?? throw new ExecParseException($"Type not found: '{typeName}'. Ensure the type is loaded in the current AppDomain.");

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => string.Equals(method.Name, methodName, StringComparison.Ordinal))
                .ToArray();

            if (methods.Length == 0)
                throw new ExecParseException($"No public static method '{methodName}' on {type.FullName}.");

            var rawArgs = ParseArguments(argsJson);
            var match = methods.FirstOrDefault(method => method.GetParameters().Length == rawArgs.Length)
                ?? methods[0];

            var parameters = match.GetParameters();
            if (parameters.Length != rawArgs.Length)
            {
                throw new ExecParseException(
                    $"Method '{methodName}' on {type.FullName} expects {parameters.Length} argument(s), but received {rawArgs.Length}.");
            }

            var convertedArgs = new object?[rawArgs.Length];
            for (var i = 0; i < rawArgs.Length; i++)
                convertedArgs[i] = ConvertToken(rawArgs[i], parameters[i].ParameterType);

            return match.Invoke(null, convertedArgs);
        }

        private static object? GetMember(string expr)
        {
            var (type, memberName) = ResolveTypeMember(expr);
            var prop = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Static);
            if (prop != null) return prop.GetValue(null);

            var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Static);
            if (field != null) return field.GetValue(null);

            throw new ExecParseException($"No public static property or field '{memberName}' on {type.FullName}.");
        }

        private static object? SetMember(string lhs, string rhs)
        {
            var (type, memberName) = ResolveTypeMember(lhs);
            var prop = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Static);
            if (prop != null)
            {
                var value = ConvertStringValue(rhs, prop.PropertyType);
                prop.SetValue(null, value);
                return value;
            }

            var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Static);
            if (field != null)
            {
                var value = ConvertStringValue(rhs, field.FieldType);
                field.SetValue(null, value);
                return value;
            }

            throw new ExecParseException($"No settable public static property or field '{memberName}' on {type.FullName}.");
        }

        private static object? InvokeExpression(string expr)
        {
            var parenIdx = expr.IndexOf('(');
            var methodPath = expr.Substring(0, parenIdx).Trim();
            var argsStr = expr.Substring(parenIdx + 1).TrimEnd(')').Trim();

            var (type, methodName) = ResolveTypeMember(methodPath);
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == methodName)
                .ToArray();

            if (methods.Length == 0)
                throw new ExecParseException($"No public static method '{methodName}' on {type.FullName}.");

            var args = string.IsNullOrWhiteSpace(argsStr)
                ? Array.Empty<string>()
                : argsStr.Split(',').Select(a => a.Trim()).ToArray();

            var method = methods.FirstOrDefault(m => m.GetParameters().Length == args.Length)
                ?? methods[0];
            var parameters = method.GetParameters();
            var convertedArgs = new object?[Math.Min(args.Length, parameters.Length)];

            for (var i = 0; i < convertedArgs.Length; i++)
                convertedArgs[i] = ConvertStringValue(args[i], parameters[i].ParameterType);

            return method.Invoke(null, convertedArgs);
        }

        private static (Type type, string memberName) ResolveTypeMember(string expr)
        {
            var lastDot = expr.LastIndexOf('.');
            if (lastDot < 0)
                throw new ExecParseException($"Expression must be 'TypeName.MemberName', got: {expr}");

            var typePart = expr.Substring(0, lastDot).Trim();
            var memberName = expr.Substring(lastDot + 1).Trim();
            var type = ResolveType(typePart)
                ?? throw new ExecParseException($"Type not found: '{typePart}'. Ensure the type is loaded in the current AppDomain.");

            return (type, memberName);
        }

        internal static Type? ResolveType(string typeName)
        {
            foreach (var blocked in BlockedTypePatterns)
            {
                if (typeName.StartsWith(blocked, StringComparison.OrdinalIgnoreCase))
                    throw new ExecSecurityException($"Type '{typeName}' is blocked for safety.");
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                var exact = asm.GetType(typeName, throwOnError: false, ignoreCase: false);
                if (exact != null)
                    return exact;
            }

            var shortMatches = assemblies
                .Where(assembly => !assembly.IsDynamic)
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        return ex.Types.Where(type => type != null);
                    }
                })
                .Where(type => type != null
                               && string.Equals(type.Name, typeName, StringComparison.Ordinal)
                               && IsCallableType(type))
                .Cast<Type>()
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
                IEnumerable<Type> types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(type => type != null);
                }

                foreach (var type in types)
                {
                    if (type == null || !IsCallableType(type))
                        continue;

                    yield return type;
                }
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

        private static bool MatchesFilter(Type type, string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            return type.FullName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                   || type.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                   || type.Assembly.GetName().Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static JToken[] ParseArguments(string? argsJson)
        {
            if (string.IsNullOrWhiteSpace(argsJson))
                return Array.Empty<JToken>();

            var token = JToken.Parse(argsJson);
            if (token is not JArray array)
                throw new ExecParseException("Parameter 'args' must be a JSON array.");

            return array.ToArray();
        }

        private static object? ConvertToken(JToken token, Type targetType)
        {
            if (token.Type == JTokenType.Null)
                return null;

            if (targetType == typeof(string))
                return token.Value<string>();

            if (targetType == typeof(bool))
                return token.Value<bool>();

            if (targetType == typeof(int))
                return token.Value<int>();

            if (targetType == typeof(float))
                return token.Value<float>();

            if (targetType == typeof(double))
                return token.Value<double>();

            if (targetType.IsEnum)
                return Enum.Parse(targetType, token.Value<string>() ?? string.Empty, ignoreCase: true);

            return token.ToObject(targetType);
        }

        private static object? ConvertStringValue(string raw, Type targetType)
        {
            if (targetType == typeof(bool))
            {
                return bool.TryParse(raw, out var b) ? b
                    : raw == "1" ? true
                    : raw == "0" ? (object)false
                    : throw new ExecParseException($"Cannot convert '{raw}' to bool.");
            }

            if (targetType == typeof(int)) return int.Parse(raw, CultureInfo.InvariantCulture);
            if (targetType == typeof(float)) return float.Parse(raw, CultureInfo.InvariantCulture);
            if (targetType == typeof(double)) return double.Parse(raw, CultureInfo.InvariantCulture);
            if (targetType == typeof(string)) return raw.Trim('"').Trim('\'');
            if (targetType.IsEnum) return Enum.Parse(targetType, raw.Trim('"').Trim('\''), ignoreCase: true);

            return Convert.ChangeType(raw, targetType, CultureInfo.InvariantCulture);
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
}
#endif
