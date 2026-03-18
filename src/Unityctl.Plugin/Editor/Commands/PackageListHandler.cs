using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class PackageListHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.PackageList;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var listRequest = UnityEditor.PackageManager.Client.List(true, true);

            while (!listRequest.IsCompleted)
                System.Threading.Thread.Sleep(50);

            if (listRequest.Status == UnityEditor.PackageManager.StatusCode.Failure)
            {
                return Fail(StatusCode.UnknownError,
                    $"Package list failed: {listRequest.Error?.message ?? "unknown error"}");
            }

            var packages = new JArray();
            foreach (var pkg in listRequest.Result)
            {
                packages.Add(new JObject
                {
                    ["name"] = pkg.name,
                    ["version"] = pkg.version,
                    ["source"] = pkg.source.ToString()
                });
            }

            return Ok($"Found {packages.Count} packages", new JObject
            {
                ["packages"] = packages,
                ["count"] = packages.Count
            });
#else
            return NotInEditor();
#endif
        }
    }
}
