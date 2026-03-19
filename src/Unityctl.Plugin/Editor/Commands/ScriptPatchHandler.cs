using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ScriptPatchHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ScriptPatch;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var path = request.GetParam("path", null);
            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");

            if (!path.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase))
                return InvalidParameters("Path must end with .cs");

            if (!path.StartsWith("Assets/") && !path.StartsWith("Assets\\"))
                return InvalidParameters("Path must be under Assets/");

            var fullPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath), path);
            if (!System.IO.File.Exists(fullPath))
                return Fail(StatusCode.NotFound, $"Script not found: {path}");

            // Parse patch parameters
            var startLineStr = request.GetParam("startLine", null);
            if (string.IsNullOrEmpty(startLineStr))
                return InvalidParameters("Parameter 'startLine' is required (1-based line number).");

            if (!int.TryParse(startLineStr, out var startLine) || startLine < 0)
                return InvalidParameters("'startLine' must be a non-negative integer (0 = insert at beginning).");

            var deleteCountStr = request.GetParam("deleteCount", "0");
            if (!int.TryParse(deleteCountStr, out var deleteCount) || deleteCount < 0)
                return InvalidParameters("'deleteCount' must be a non-negative integer.");

            var insertContent = request.GetParam("insertContent", null);

            if (deleteCount == 0 && insertContent == null)
                return InvalidParameters("At least one of 'deleteCount' > 0 or 'insertContent' must be provided.");

            // Read file lines
            var lines = new System.Collections.Generic.List<string>(
                System.IO.File.ReadAllLines(fullPath));

            // Validate line range
            if (startLine > lines.Count + 1)
                return InvalidParameters(
                    $"'startLine' ({startLine}) exceeds file length ({lines.Count} lines). Use {lines.Count + 1} to append.");

            if (deleteCount > 0)
            {
                if (startLine < 1)
                    return InvalidParameters("'startLine' must be >= 1 when deleting lines.");

                if (startLine + deleteCount - 1 > lines.Count)
                    return InvalidParameters(
                        $"Cannot delete {deleteCount} lines starting at {startLine}: file has only {lines.Count} lines.");

                lines.RemoveRange(startLine - 1, deleteCount);
            }

            // Insert content
            var insertedLineCount = 0;
            if (!string.IsNullOrEmpty(insertContent))
            {
                var newLines = insertContent.Split(new[] { "\n" }, System.StringSplitOptions.None);
                insertedLineCount = newLines.Length;

                var insertIndex = startLine > 0 ? startLine - 1 : 0;
                if (insertIndex > lines.Count) insertIndex = lines.Count;

                lines.InsertRange(insertIndex, newLines);
            }

            // Write back
            var result = string.Join("\n", lines);
            if (lines.Count > 0 && !result.EndsWith("\n"))
                result += "\n";

            System.IO.File.WriteAllText(fullPath, result, System.Text.Encoding.UTF8);
            UnityEditor.AssetDatabase.ImportAsset(path);

            return Ok($"Patched script at '{path}' (deleted {deleteCount}, inserted {insertedLineCount} lines)", new JObject
            {
                ["path"] = path,
                ["linesDeleted"] = deleteCount,
                ["linesInserted"] = insertedLineCount,
                ["totalLines"] = lines.Count,
                ["bytesWritten"] = System.Text.Encoding.UTF8.GetByteCount(result)
            });
#else
            return NotInEditor();
#endif
        }
    }
}
