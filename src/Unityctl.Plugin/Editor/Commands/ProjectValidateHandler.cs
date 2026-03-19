using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ProjectValidateHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ProjectValidate;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var checks = new List<JObject>();

            // 1. Compilation status
            CheckCompilation(checks);

            // 2. Build scenes configured
            CheckBuildScenes(checks);

            // 3. Camera exists
            CheckCamera(checks);

            // 4. Light exists
            CheckLight(checks);

            // 5. Console errors
            CheckConsoleErrors(checks);

            // 6. Editor state
            CheckEditorState(checks);

            var passCount = checks.Count(c => c["pass"]?.Value<bool>() == true);
            var failCount = checks.Count - passCount;
            var errorFails = checks.Count(c =>
                c["pass"]?.Value<bool>() == false &&
                c["severity"]?.Value<string>() == "error");

            var data = new JObject
            {
                ["valid"] = errorFails == 0,
                ["checks"] = new JArray(checks),
                ["passCount"] = passCount,
                ["failCount"] = failCount,
                ["totalChecks"] = checks.Count
            };

            if (errorFails > 0)
                return Ok($"Validation failed: {errorFails} error(s), {failCount - errorFails} warning(s)", data);

            return Ok($"All {passCount} checks passed", data);
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        private static void CheckCompilation(List<JObject> checks)
        {
            var isCompiling = UnityEditor.EditorApplication.isCompiling;
            var failed = UnityEditor.EditorUtility.scriptCompilationFailed;

            if (isCompiling)
            {
                checks.Add(MakeCheck("compile", "error", false, "Scripts are still compiling"));
                return;
            }

            if (failed)
            {
                // Try to get error details from collector
                var result = ScriptCompilationCollector.GetLatestResult();
                var errorCount = result?["errorCount"]?.Value<int>() ?? 0;
                checks.Add(MakeCheck("compile", "error", false,
                    $"Compilation failed with {errorCount} error(s). Run 'script-get-errors' for details."));
                return;
            }

            checks.Add(MakeCheck("compile", "error", true, "No compilation errors"));
        }

        private static void CheckBuildScenes(List<JObject> checks)
        {
            var scenes = UnityEditor.EditorBuildSettings.scenes;
            var enabled = scenes.Where(s => s.enabled).ToArray();

            if (enabled.Length == 0)
            {
                checks.Add(MakeCheck("buildScenes", "error", false,
                    "No scenes enabled in Build Settings"));
                return;
            }

            // Check if scene files exist
            var missing = enabled.Where(s => !System.IO.File.Exists(s.path)).ToArray();
            if (missing.Length > 0)
            {
                checks.Add(MakeCheck("buildScenes", "error", false,
                    $"{missing.Length} enabled scene(s) missing on disk: {string.Join(", ", missing.Select(s => s.path))}"));
                return;
            }

            checks.Add(MakeCheck("buildScenes", "error", true,
                $"{enabled.Length} scene(s) enabled in Build Settings"));
        }

        private static void CheckCamera(List<JObject> checks)
        {
            var camera = UnityEngine.Camera.main;
            if (camera == null)
                camera = UnityEngine.Object.FindObjectOfType<UnityEngine.Camera>();

            if (camera == null)
            {
                checks.Add(MakeCheck("camera", "error", false, "No Camera found in loaded scenes"));
                return;
            }

            checks.Add(MakeCheck("camera", "error", true,
                $"Camera found: {camera.gameObject.name}"));
        }

        private static void CheckLight(List<JObject> checks)
        {
            var light = UnityEngine.Object.FindObjectOfType<UnityEngine.Light>();

            if (light == null)
            {
                checks.Add(MakeCheck("light", "warning", false, "No Light found in loaded scenes"));
                return;
            }

            checks.Add(MakeCheck("light", "warning", true,
                $"Light found: {light.gameObject.name} ({light.type})"));
        }

        private static void CheckConsoleErrors(List<JObject> checks)
        {
            int errors = 0, warnings = 0;

            var logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor");
            if (logEntriesType != null)
            {
                var method = logEntriesType.GetMethod("GetCountsByType",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method != null)
                {
                    var args = new object[] { 0, 0, 0 };
                    method.Invoke(null, args);
                    errors = (int)args[0];
                    warnings = (int)args[1];
                }
            }

            if (errors > 0)
            {
                checks.Add(MakeCheck("consoleErrors", "warning", false,
                    $"{errors} error(s) in console"));
                return;
            }

            checks.Add(MakeCheck("consoleErrors", "warning", true,
                $"Console clean ({warnings} warning(s))"));
        }

        private static void CheckEditorState(List<JObject> checks)
        {
            var isPlaying = UnityEditor.EditorApplication.isPlaying;
            if (isPlaying)
            {
                checks.Add(MakeCheck("editorState", "warning", false,
                    "Editor is in Play Mode (exit before building)"));
                return;
            }

            checks.Add(MakeCheck("editorState", "warning", true, "Editor is in Edit Mode"));
        }

        private static JObject MakeCheck(string name, string severity, bool pass, string detail)
        {
            return new JObject
            {
                ["name"] = name,
                ["severity"] = severity,
                ["pass"] = pass,
                ["detail"] = detail
            };
        }
#endif
    }
}
