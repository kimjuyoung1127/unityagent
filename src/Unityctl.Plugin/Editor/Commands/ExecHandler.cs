#if UNITY_EDITOR
using System;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    /// <summary>
    /// Handles the "exec" command: evaluates a C# expression in the Unity Editor
    /// using reflection. Supports property get/set and method calls on any type
    /// loaded in the current AppDomain.
    ///
    /// Security: dangerous types (System.IO.File, System.Diagnostics.Process,
    /// System.Net.*, System.Reflection.Emit) are blocked via BlockedTypePatterns.
    /// All other types — including project code — are allowed.
    /// This handler assumes a trusted agent caller.
    /// </summary>
    public class ExecHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.Exec;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var code = request.GetParam("code");
            if (string.IsNullOrWhiteSpace(code))
                return InvalidParameters("'code' parameter is required.");

            try
            {
                var result = ExecReflectionUtility.EvaluateExpression(code.Trim());
                var data = new JObject { ["result"] = result != null ? JToken.FromObject(result) : JValue.CreateNull() };
                return Ok($"exec: {code}", data);
            }
            catch (ExecSecurityException ex)
            {
                return Fail(StatusCode.InvalidParameters, $"Security violation: {ex.Message}");
            }
            catch (ExecParseException ex)
            {
                return Fail(StatusCode.InvalidParameters, $"Parse error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Fail(StatusCode.UnknownError, $"Execution error: {ex.Message}");
            }
        }
    }
}
#endif
