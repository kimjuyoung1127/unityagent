#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class CameraListHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.CameraList;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var includeInactive = request.GetParam<bool>("includeInactive");

            var cameras = includeInactive
                ? UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.Camera>()
                : UnityEngine.Object.FindObjectsOfType<UnityEngine.Camera>();

            var results = new JArray();
            foreach (var cam in cameras)
            {
                if (cam == null || cam.gameObject == null)
                    continue;

                // Skip scene-internal cameras (preview, etc.)
                if (cam.gameObject.hideFlags != UnityEngine.HideFlags.None)
                    continue;

                // For FindObjectsOfTypeAll, skip non-scene objects
                if (includeInactive && cam.gameObject.scene.name == null)
                    continue;

                var entry = new JObject
                {
                    ["globalObjectId"] = GlobalObjectIdResolver.GetId(cam),
                    ["gameObjectGlobalObjectId"] = GlobalObjectIdResolver.GetId(cam.gameObject),
                    ["gameObjectName"] = cam.gameObject.name,
                    ["isMain"] = cam == UnityEngine.Camera.main,
                    ["enabled"] = cam.enabled,
                    ["tag"] = cam.gameObject.tag,
                    ["depth"] = cam.depth,
                    ["hierarchyPath"] = SceneExplorationUtility.GetHierarchyPath(cam.gameObject)
                };

                results.Add(entry);
            }

            return Ok($"Found {results.Count} camera(s)", new JObject
            {
                ["cameras"] = results
            });
        }
    }
}
#endif
