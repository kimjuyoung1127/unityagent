using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;
using Unityctl.Core.Sessions;
using Unityctl.Core.Transport;

var builder = Host.CreateApplicationBuilder(args);

// Core services for DI injection into MCP tool classes
builder.Services.AddSingleton<IPlatformServices>(_ => PlatformFactory.Create());
builder.Services.AddSingleton<UnityEditorDiscovery>();
builder.Services.AddSingleton<CommandExecutor>();
builder.Services.AddSingleton<SessionManager>(
    _ => new SessionManager(new NdjsonSessionStore()));

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly()
    .WithPromptsFromAssembly();

await builder.Build().RunAsync();
