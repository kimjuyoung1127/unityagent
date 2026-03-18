using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class MaterialSetShaderHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.MaterialSetShader;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var path = request.GetParam("path", null);
            var shaderName = request.GetParam("shader", null);

            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");
            if (string.IsNullOrEmpty(shaderName))
                return InvalidParameters("Parameter 'shader' is required.");

            var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
            if (mat == null)
                return Fail(StatusCode.NotFound, $"Material not found at: {path}");

            var shader = UnityEngine.Shader.Find(shaderName);
            if (shader == null)
                return Fail(StatusCode.NotFound, $"Shader not found: {shaderName}");

            var undoName = $"unityctl: material-set-shader: {shaderName}";
            UnityEditor.Undo.RecordObject(mat, undoName);

            mat.shader = shader;

            UnityEditor.EditorUtility.SetDirty(mat);
            UnityEditor.AssetDatabase.SaveAssets();

            return Ok($"Set shader to '{shaderName}' on '{path}'", new JObject
            {
                ["path"] = path,
                ["shader"] = shader.name,
                ["undoGroupName"] = undoName
            });
#else
            return NotInEditor();
#endif
        }
    }
}
