using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class PingHandler : IUnityctlCommand
    {
        public string CommandName => WellKnownCommands.Ping;

        public CommandResponse Execute(CommandRequest request)
        {
            var data = new JObject
            {
                ["version"] = "0.3.2",
#if UNITY_EDITOR
                ["unityVersion"] = UnityEngine.Application.unityVersion
#endif
            };
            return CommandResponse.Ok("pong", data);
        }
    }
}
