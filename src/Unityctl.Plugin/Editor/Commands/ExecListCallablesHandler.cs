#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityEditor;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ExecListCallablesHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ExecListCallables;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var filter = request.GetParam("filter", null);
            var limit = request.GetParam("limit", 100);
            if (limit <= 0)
                limit = 100;

            var results = ExecReflectionUtility.ListCallables(filter, limit);
            return Ok($"Found {results.Count} callable type(s)", new JObject
            {
                ["filter"] = filter,
                ["limit"] = limit,
                ["results"] = results,
                ["count"] = results.Count,
                ["compiledAt"] = ScriptCompilationCollector.GetLatestResult()?["compiledAt"]?.Value<string>(),
                ["domainStable"] = !EditorApplication.isCompiling && !EditorApplication.isUpdating
            });
        }
    }
}
#endif
