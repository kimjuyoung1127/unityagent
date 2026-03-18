// ★ Plugin-side CommandRequest — uses Newtonsoft JObject for parameters
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Unityctl.Plugin.Editor.Shared
{
    [Serializable]
    public class CommandRequest
    {
        [JsonProperty("command")]
        public string command = string.Empty;

        [JsonProperty("parameters")]
        public JObject parameters;

        [JsonProperty("requestId")]
        public string requestId = string.Empty;

        /// <summary>
        /// Get a string parameter. Returns defaultValue if missing or null.
        /// </summary>
        public string GetParam(string key, string defaultValue = null)
            => parameters?.Value<string>(key) ?? defaultValue;

        /// <summary>
        /// Get a typed parameter (int, bool, long, double, etc).
        /// Returns defaultValue if missing, null, or wrong type.
        /// </summary>
        public T GetParam<T>(string key, T defaultValue = default) where T : struct
        {
            if (parameters == null) return defaultValue;
            var token = parameters[key];
            if (token == null || token.Type == JTokenType.Null)
                return defaultValue;
            try
            {
                return token.Value<T>();
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Get a nested JObject parameter.
        /// Returns null if missing or not an object.
        /// </summary>
        public JObject GetObjectParam(string key)
        {
            return parameters?[key] as JObject;
        }
    }
}
