using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Unityctl.Core.Sessions;
using Unityctl.Shared.Serialization;

namespace Unityctl.Mcp.Tools;

[McpServerToolType]
internal sealed class SessionTool(SessionManager sessions)
{
    [McpServerTool(Name = "unityctl_session_list")]
    [Description("List active and recent sessions")]
    public async Task<string> SessionListAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await sessions.ListAsync(cancellationToken);
        return JsonSerializer.Serialize(list.ToArray(), UnityctlJsonContext.Default.SessionArray);
    }
}
