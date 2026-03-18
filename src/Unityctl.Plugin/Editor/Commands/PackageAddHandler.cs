using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class PackageAddHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.PackageAdd;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var package = request.GetParam("package", null);
            if (string.IsNullOrEmpty(package))
                return InvalidParameters("Parameter 'package' is required.");

            var addRequest = UnityEditor.PackageManager.Client.Add(package);

            while (!addRequest.IsCompleted)
                System.Threading.Thread.Sleep(50);

            if (addRequest.Status == UnityEditor.PackageManager.StatusCode.Failure)
            {
                return Fail(StatusCode.UnknownError,
                    $"Package add failed: {addRequest.Error?.message ?? "unknown error"}");
            }

            var result = addRequest.Result;
            return Ok($"Added package '{result.name}@{result.version}'", new JObject
            {
                ["name"] = result.name,
                ["version"] = result.version,
                ["source"] = result.source.ToString()
            });
#else
            return NotInEditor();
#endif
        }
    }
}
