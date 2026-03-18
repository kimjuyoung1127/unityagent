using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class AnimationCreateControllerHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AnimationCreateController;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var path = request.GetParam("path", null);
            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");

            var controller = UnityEditor.Animations.AnimatorController
                .CreateAnimatorControllerAtPath(path);

            if (controller == null)
                return Fail(StatusCode.UnknownError,
                    $"Failed to create AnimatorController at: {path}");

            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
                return Fail(StatusCode.UnknownError,
                    $"AnimatorController created but GUID lookup failed for: {path}");

            return Ok($"Created AnimatorController at '{path}'", new JObject
            {
                ["path"] = path,
                ["guid"] = guid
            });
#else
            return NotInEditor();
#endif
        }
    }
}
