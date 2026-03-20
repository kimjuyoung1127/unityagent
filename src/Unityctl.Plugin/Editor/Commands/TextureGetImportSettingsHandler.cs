#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityEditor;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class TextureGetImportSettingsHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.TextureGetImportSettings;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var path = request.GetParam("path", null);
            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return Fail(StatusCode.NotFound, $"TextureImporter not found at: {path}");

            var data = new JObject
            {
                ["path"] = path,
                ["guid"] = AssetDatabase.AssetPathToGUID(path),
                ["textureType"] = importer.textureType.ToString(),
                ["textureShape"] = importer.textureShape.ToString(),
                ["maxTextureSize"] = importer.maxTextureSize,
                ["textureCompression"] = importer.textureCompression.ToString(),
                ["filterMode"] = importer.filterMode.ToString(),
                ["wrapMode"] = importer.wrapMode.ToString(),
                ["mipmapEnabled"] = importer.mipmapEnabled,
                ["isReadable"] = importer.isReadable,
                ["sRGBTexture"] = importer.sRGBTexture,
                ["alphaSource"] = importer.alphaSource.ToString(),
                ["alphaIsTransparency"] = importer.alphaIsTransparency,
                ["spriteImportMode"] = importer.spriteImportMode.ToString(),
                ["spritePixelsPerUnit"] = importer.spritePixelsPerUnit
            };

            return Ok($"Texture import settings for '{path}'", data);
        }
    }
}
#endif
