#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class UitkFindHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.UitkFind;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            if (UitkElementResolver.FindUidocumentType() == null)
                return Fail(StatusCode.NotFound, "UI Toolkit (UIDocument) not available in this Unity version.");

            var nameFilter = request.GetParam("name", null);
            var classNameFilter = request.GetParam("className", null);
            var typeFilter = request.GetParam("type", null);
            var limit = request.GetParam<int>("limit");
            if (limit <= 0)
                limit = 0;

            var results = UitkElementResolver.Find(nameFilter, classNameFilter, typeFilter, limit);
            var json = new JArray();
            foreach (var match in results)
                json.Add(UitkElementResolver.ToSummary(match));

            return Ok($"Found {json.Count} UI Toolkit element(s)", new JObject
            {
                ["results"] = json
            });
        }
    }
}
#endif
