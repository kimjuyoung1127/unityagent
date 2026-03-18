// ★ Plugin-side CommandResponse — uses Newtonsoft JObject for data
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Unityctl.Plugin.Editor.Shared
{
    [Serializable]
    public class CommandResponse
    {
        [JsonProperty("statusCode")]
        public int statusCode;

        [JsonProperty("success")]
        public bool success;

        [JsonProperty("message")]
        public string message;

        [JsonProperty("data")]
        public JObject data;

        [JsonProperty("errors")]
        public List<string> errors;

        [JsonProperty("requestId")]
        public string requestId;

        public static CommandResponse Ok(string message = null, JObject data = null)
        {
            return new CommandResponse
            {
                statusCode = (int)StatusCode.Ready,
                success = true,
                message = message,
                data = data
            };
        }

        public static CommandResponse Fail(StatusCode code, string message, List<string> errors = null)
        {
            return new CommandResponse
            {
                statusCode = (int)code,
                success = false,
                message = message,
                errors = errors
            };
        }
    }
}
