using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class AssetCreateFolderHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AssetCreateFolder;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var parent = request.GetParam("parent", null);
            if (string.IsNullOrEmpty(parent))
                return InvalidParameters("Parameter 'parent' is required.");

            var name = request.GetParam("name", null);
            if (string.IsNullOrEmpty(name))
                return InvalidParameters("Parameter 'name' is required.");

            if (!UnityEditor.AssetDatabase.IsValidFolder(parent))
                return Fail(StatusCode.NotFound, $"Parent folder not found: {parent}");

            var guid = UnityEditor.AssetDatabase.CreateFolder(parent, name);
            if (string.IsNullOrEmpty(guid))
                return Fail(StatusCode.UnknownError, $"Failed to create folder '{name}' in '{parent}'.");

            var folderPath = parent.TrimEnd('/') + "/" + name;

            return Ok($"Created folder '{folderPath}'", new JObject
            {
                ["path"] = folderPath,
                ["guid"] = guid
            });
#else
            return NotInEditor();
#endif
        }
    }
}
