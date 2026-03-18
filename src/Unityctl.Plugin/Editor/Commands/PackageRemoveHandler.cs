using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class PackageRemoveHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.PackageRemove;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var package = request.GetParam("package", null);
            if (string.IsNullOrEmpty(package))
                return InvalidParameters("Parameter 'package' is required.");

            var removeRequest = UnityEditor.PackageManager.Client.Remove(package);

            while (!removeRequest.IsCompleted)
                System.Threading.Thread.Sleep(50);

            if (removeRequest.Status == UnityEditor.PackageManager.StatusCode.Failure)
            {
                return Fail(StatusCode.UnknownError,
                    $"Package remove failed: {removeRequest.Error?.message ?? "unknown error"}");
            }

            return Ok($"Removed package '{package}'", new JObject
            {
                ["package"] = package
            });
#else
            return NotInEditor();
#endif
        }
    }
}
