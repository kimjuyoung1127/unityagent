#if UNITY_EDITOR
using System;
using Newtonsoft.Json.Linq;
using UnityEditor;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class TextureSetImportSettingsHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.TextureSetImportSettings;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var path = request.GetParam("path", null);
            var property = request.GetParam("property", null);
            var value = request.GetParam("value", null);

            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");
            if (string.IsNullOrEmpty(property))
                return InvalidParameters("Parameter 'property' is required.");
            if (value == null)
                return InvalidParameters("Parameter 'value' is required.");

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return Fail(StatusCode.NotFound, $"TextureImporter not found at: {path}");

            try
            {
                SetProperty(importer, property, value);
            }
            catch (ArgumentException ex)
            {
                return InvalidParameters(ex.Message);
            }

            importer.SaveAndReimport();

            return Ok($"Set '{property}' = '{value}' on '{path}'", new JObject
            {
                ["path"] = path,
                ["property"] = property,
                ["value"] = value
            });
        }

        private static void SetProperty(TextureImporter importer, string property, string value)
        {
            switch (property)
            {
                case "maxTextureSize":
                    importer.maxTextureSize = int.Parse(value);
                    break;
                case "textureCompression":
                    importer.textureCompression = (TextureImporterCompression)Enum.Parse(
                        typeof(TextureImporterCompression), value, true);
                    break;
                case "filterMode":
                    importer.filterMode = (UnityEngine.FilterMode)Enum.Parse(
                        typeof(UnityEngine.FilterMode), value, true);
                    break;
                case "wrapMode":
                    importer.wrapMode = (UnityEngine.TextureWrapMode)Enum.Parse(
                        typeof(UnityEngine.TextureWrapMode), value, true);
                    break;
                case "mipmapEnabled":
                    importer.mipmapEnabled = bool.Parse(value);
                    break;
                case "isReadable":
                    importer.isReadable = bool.Parse(value);
                    break;
                case "sRGBTexture":
                    importer.sRGBTexture = bool.Parse(value);
                    break;
                case "textureType":
                    importer.textureType = (TextureImporterType)Enum.Parse(
                        typeof(TextureImporterType), value, true);
                    break;
                case "textureShape":
                    importer.textureShape = (TextureImporterShape)Enum.Parse(
                        typeof(TextureImporterShape), value, true);
                    break;
                case "alphaSource":
                    importer.alphaSource = (TextureImporterAlphaSource)Enum.Parse(
                        typeof(TextureImporterAlphaSource), value, true);
                    break;
                case "alphaIsTransparency":
                    importer.alphaIsTransparency = bool.Parse(value);
                    break;
                case "spriteImportMode":
                    importer.spriteImportMode = (SpriteImportMode)Enum.Parse(
                        typeof(SpriteImportMode), value, true);
                    break;
                case "spritePixelsPerUnit":
                    importer.spritePixelsPerUnit = float.Parse(value);
                    break;
                default:
                    throw new ArgumentException($"Unknown TextureImporter property: '{property}'");
            }
        }
    }
}
#endif
