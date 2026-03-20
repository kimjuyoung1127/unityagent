#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class CameraGetHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.CameraGet;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var id = request.GetParam("id", null);
            if (string.IsNullOrEmpty(id))
                return InvalidParameters("Parameter 'id' is required.");

            // Try resolving as Camera first, then as GameObject
            var cam = GlobalObjectIdResolver.Resolve<UnityEngine.Camera>(id);
            if (cam == null)
            {
                var go = GlobalObjectIdResolver.Resolve<UnityEngine.GameObject>(id);
                if (go != null)
                    cam = go.GetComponent<UnityEngine.Camera>();
            }

            if (cam == null)
                return Fail(StatusCode.NotFound, $"Camera not found: {id}");

            var data = new JObject
            {
                ["globalObjectId"] = GlobalObjectIdResolver.GetId(cam),
                ["gameObjectGlobalObjectId"] = GlobalObjectIdResolver.GetId(cam.gameObject),
                ["gameObjectName"] = cam.gameObject.name,
                ["hierarchyPath"] = SceneExplorationUtility.GetHierarchyPath(cam.gameObject),
                ["isMain"] = cam == UnityEngine.Camera.main,
                ["enabled"] = cam.enabled,
                ["fieldOfView"] = cam.fieldOfView,
                ["nearClipPlane"] = cam.nearClipPlane,
                ["farClipPlane"] = cam.farClipPlane,
                ["orthographic"] = cam.orthographic,
                ["orthographicSize"] = cam.orthographicSize,
                ["clearFlags"] = cam.clearFlags.ToString(),
                ["backgroundColor"] = new JObject
                {
                    ["r"] = cam.backgroundColor.r,
                    ["g"] = cam.backgroundColor.g,
                    ["b"] = cam.backgroundColor.b,
                    ["a"] = cam.backgroundColor.a
                },
                ["cullingMask"] = cam.cullingMask,
                ["depth"] = cam.depth,
                ["renderingPath"] = cam.renderingPath.ToString(),
                ["targetDisplay"] = cam.targetDisplay,
                ["rect"] = new JObject
                {
                    ["x"] = cam.rect.x,
                    ["y"] = cam.rect.y,
                    ["width"] = cam.rect.width,
                    ["height"] = cam.rect.height
                },
                ["allowHDR"] = cam.allowHDR,
                ["allowMSAA"] = cam.allowMSAA
            };

            return Ok($"Camera '{cam.gameObject.name}'", data);
        }
    }
}
#endif
