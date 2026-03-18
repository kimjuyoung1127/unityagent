using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class AnimationCreateClipHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AnimationCreateClip;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var path = request.GetParam("path", null);
            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");

            var clip = new UnityEngine.AnimationClip();
            UnityEditor.AssetDatabase.CreateAsset(clip, path);
            UnityEditor.AssetDatabase.SaveAssets();

            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
                return Fail(StatusCode.UnknownError,
                    $"AnimationClip created but GUID lookup failed for: {path}");

            return Ok($"Created AnimationClip at '{path}'", new JObject
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
