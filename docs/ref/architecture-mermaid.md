# unityctl Architecture

## Project Structure

```
unityctl.slnx
├── src/Unityctl.Shared   (netstandard2.1)  Protocol, models, constants
├── src/Unityctl.Core     (net10.0)         Business logic (transport, discovery, retry)
├── src/Unityctl.Cli      (net10.0)         CLI shell → dotnet tool "unityctl"
├── src/Unityctl.Mcp      (net10.0)         MCP server → dotnet tool "unityctl-mcp"
├── src/Unityctl.Plugin   (Unity UPM)       Editor bridge (IPC server)
└── tests/*                                 633 xUnit tests
```

## Dependency Direction

```mermaid
graph LR
    Shared["Unityctl.Shared<br/>(netstandard2.1)"]
    Core["Unityctl.Core<br/>(net10.0)"]
    Cli["Unityctl.Cli<br/>(net10.0)"]
    Mcp["Unityctl.Mcp<br/>(net10.0)"]
    Plugin["Unityctl.Plugin<br/>(Unity UPM)"]

    Shared --> Core --> Cli
    Core --> Mcp
    Shared -.->|source copy| Plugin
```

## Transport Flow

```mermaid
sequenceDiagram
    participant CLI as unityctl CLI
    participant Executor as CommandExecutor
    participant IPC as IpcTransport
    participant Batch as BatchTransport
    participant Editor as Unity Editor

    CLI->>Executor: ExecuteAsync(request)
    Executor->>IPC: ProbeAsync()
    alt IPC server running
        IPC-->>Executor: true
        Executor->>IPC: SendAsync(request)
        IPC->>Editor: Named Pipe (4-byte LE + JSON)
        Editor->>Editor: IpcServer → MainThread → Handler
        Editor-->>IPC: CommandResponse
        IPC-->>Executor: response
    else IPC unavailable
        IPC-->>Executor: false
        Executor->>Batch: SendAsync(request)
        Batch->>Editor: Unity -batchmode -executeMethod
        Editor-->>Batch: response-file JSON
        Batch-->>Executor: response
    end
    Executor-->>CLI: CommandResponse
```

## IPC Server (Plugin)

```mermaid
graph TD
    subgraph "Background Thread"
        Listen["ListenLoop()"]
        Pipe["NamedPipeServerStream"]
        Read["MessageFraming.ReadMessage()"]
    end

    subgraph "Main Thread (EditorApplication.update)"
        Pump["PumpMainThreadQueue()"]
        Route["IpcRequestRouter.Route()"]
        Handler["CommandHandler.Execute()"]
    end

    Listen --> Pipe --> Read
    Read -->|ConcurrentQueue| Pump
    Pump --> Route --> Handler
    Handler -->|ManualResetEventSlim.Set()| Listen
```

## MCP Server

```mermaid
graph LR
    Agent["AI Agent<br/>(Claude, Cursor)"]
    McpServer["unityctl-mcp<br/>(stdio)"]
    Core2["Unityctl.Core"]
    Editor2["Unity Editor"]

    Agent -->|MCP protocol| McpServer
    McpServer -->|CommandExecutor| Core2
    Core2 -->|IPC / Batch| Editor2
```

12 MCP tools including `unityctl_query`, `unityctl_run`, `unityctl_schema`, `unityctl_status`, `unityctl_watch`, and the rest of the unified MCP surface.

## Wire Protocol

```
Frame:    [4 bytes int32 LE: payload length] [N bytes UTF-8: JSON body]
Max size: 10 MB

CommandRequest  → { command, parameters, requestId }
CommandResponse → { statusCode, success, message, data, errors, requestId }
```

## Pipe Name Generation

```
NormalizeProjectPath(path)
  → GetFullPath → lowercase (Windows) → \ → / → trim trailing /
GetPipeName(path)
  → SHA256(normalize(path)) → hex → "unityctl_" + first 16 chars
  → 25 characters total (e.g., "unityctl_a1b2c3d4e5f6a7b8")
```

## Transport Comparison

| | IPC | Batch |
|---|---|---|
| Requires running Editor | Yes | No |
| Latency | ~100ms | 30-120s |
| Platform | Named Pipe (Win) / UDS (macOS/Linux) | All |
| Streaming support | Yes (Watch Mode) | No |
| Write commands | Yes | Limited |
| CI/CD friendly | No | Yes |
