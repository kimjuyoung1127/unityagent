# unityctl

CLI tool for controlling Unity Editor — built for AI agents and CI/CD pipelines.

**9x smaller schema** than existing Unity MCP solutions. Headless CI/CD without open Editor.

```bash
# Install plugin + run a build preflight check in 3 commands
dotnet build unityctl.slnx
dotnet run --project src/Unityctl.Cli -- init --project /path/to/unity/project
dotnet run --project src/Unityctl.Cli -- build --dry-run --project /path/to/unity/project --json
```

## Terminal Output

<p align="center">
  <img src="docs/assets/editor-list.svg" alt="unityctl editor list" width="570">
</p>

<p align="center">
  <img src="docs/assets/log-table.svg" alt="unityctl log" width="645">
</p>

<p align="center">
  <img src="docs/assets/tools.svg" alt="unityctl tools" width="654">
</p>

## Why unityctl?

| Feature | unityctl | Existing Unity MCP |
|---------|----------|--------------------|
| Headless CI/CD | ✅ `check` / EditMode `test` verified without open Editor | ❌ Editor must be open |
| Token Efficiency | ✅ 5,024 B schema (9.1x smaller) | 45,705 B schema |
| Editor Discovery | ✅ auto-detect installed versions | ❌ manual path config |
| Transport Fallback | ✅ IPC → batch auto-fallback | ❌ single path |
| Native .NET MCP | ✅ C# SDK, no Python/TS bridge | Python/TS bridge |
| Preflight Validation | ✅ `--dry-run` with 19 checks | ❌ |
| Flight Recorder | ✅ NDJSON command logging | ❌ |
| Session Tracking | ✅ state machine + stale detection | ❌ |
| Real-time Streaming | ✅ `watch` console/hierarchy/compilation | ❌ |
| Scene Diff | ✅ property-level diff with epsilon | ❌ |

## Benchmarks

| Metric | unityctl (Mcp) | CoplayDev MCP |
|--------|---------------|---------------|
| Schema size | **5,024 B** | 45,705 B |
| `ping` latency | 100 ms | 1 ms |
| `editor_state` | 100 ms | 100 ms |
| `active_scene` | 99 ms | 100 ms |

- [Response-time benchmark](docs/benchmark/benchmark-results.md)
- [Token-efficiency benchmark](docs/benchmark/token-comparison.md)
- [Headless batch validation](docs/benchmark/headless-batch-validation.md)

## Write API

Phase A and Phase B typed write commands are now live-tested against a real Unity project.

Verified commands:

- `unityctl play start|stop|pause --project <path> --json`
- `unityctl player-settings get --project <path> --key productName --json`
- `unityctl player-settings set --project <path> --key companyName --value "TestCo" --json`
- `unityctl asset refresh --project <path> --json`
- `unityctl gameobject create|rename|move|delete --project <path> ... --json`
- `unityctl gameobject activate|deactivate --project <path> --id <globalObjectId> --json`
- `unityctl component add|remove|set-property --project <path> ... --json`
- `unityctl scene save --project <path> [--all] --json`

Observed behavior:

- `play start`, `play stop`, and `play pause` all work over IPC
- `player-settings set` updates `companyName` and returns an `undoGroupName`
- `asset refresh` returns a structured `"Asset refresh scheduled"` response, then IPC reconnects after refresh/reload settles
- `gameobject create` returns a `globalObjectId`, `sceneDirty`, and `undoGroupName`
- `component add` returns a `componentGlobalObjectId`, and `component set-property` works with Unity serialized property paths like `m_LocalPosition` and `m_Mass`
- `PrefabGuard` rejects prefab-instance writes with a structured v1 limitation message

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Unity 2021.3+](https://unity.com/download) via Unity Hub

### Build

```bash
git clone https://github.com/kimjuyoung1127/unityctl.git
cd unityctl
dotnet build unityctl.slnx
```

### Install Plugin

```bash
dotnet run --project src/Unityctl.Cli -- init --project /path/to/unity/project
```

This adds `com.unityctl.bridge` to your Unity project's `Packages/manifest.json`.

### Basic Commands

```bash
# List installed Unity editors
unityctl editor list

# Ping Unity (IPC if Editor open, batch otherwise)
unityctl ping --project /path/to/project

# Check compilation
unityctl check --project /path/to/project --json

# Run EditMode tests (with polling)
unityctl test --project /path/to/project --mode edit --json

# Build preflight validation
unityctl build --project /path/to/project --target StandaloneWindows64 --dry-run --json

# Execute C# expression in Unity
unityctl exec --project /path/to/project --code "Application.version"

# Stream Unity events in real-time
unityctl watch --project /path/to/project --channel console

# Phase A typed writes
unityctl play start --project /path/to/project --json
unityctl player-settings get --project /path/to/project --key productName --json
unityctl asset refresh --project /path/to/project --json

# Phase B typed writes
unityctl gameobject create --project /path/to/project --name "Cube" --json
unityctl gameobject rename --project /path/to/project --id <globalObjectId> --name "NewName" --json
unityctl gameobject move --project /path/to/project --id <childId> --parent <parentId> --json
unityctl gameobject deactivate --project /path/to/project --id <globalObjectId> --json
unityctl component add --project /path/to/project --id <globalObjectId> --type "UnityEngine.Rigidbody" --json
unityctl component set-property --project /path/to/project --component-id <componentGlobalObjectId> --property "m_Mass" --value "5" --json
unityctl scene save --project /path/to/project --json

# Machine-readable schema for AI agents
unityctl schema --format json
```

## Architecture

```
unityctl.slnx
├── src/Unityctl.Shared   (netstandard2.1)  Protocol + models
├── src/Unityctl.Core     (net10.0)         Business logic (transport, discovery, retry)
├── src/Unityctl.Cli      (net10.0)         Thin CLI shell
├── src/Unityctl.Mcp      (net10.0)         MCP server (Claude/Cursor/VS Code)
├── src/Unityctl.Plugin   (Unity UPM)       Editor bridge
└── tests/*                                 312 xUnit tests
```

### Transport

unityctl auto-selects the best transport:

1. **IPC** (Named Pipe / Unix Domain Socket) — if Unity Editor is running with plugin → ~100ms
2. **Batch** — spawns Unity in batchmode → 30-120s

### MCP Server

```bash
# Run as MCP server (stdio transport)
dotnet run --project src/Unityctl.Mcp
```

Compatible with Claude Code, Cursor, VS Code, and any MCP client.

12 MCP tool names available across 11 classes: `ping`, `status`, `build`, `test`, `check`, `exec`, `log`, `session`, `schema`, `watch`, `scene snapshot`, `scene diff`.

## Commands

| Command | Description |
|---------|-------------|
| `editor list` | List installed Unity editors |
| `init` | Install plugin to Unity project |
| `ping` | Check Unity connectivity |
| `status` | Get editor state (compiling, playing, etc.) |
| `check` | Verify script compilation |
| `test` | Run EditMode/PlayMode tests |
| `build` | Build player (with `--dry-run` preflight) |
| `play` | Control Unity play mode (`start`, `stop`, `pause`) |
| `player-settings` | Read or update selected `PlayerSettings` values |
| `asset` | Trigger async AssetDatabase refresh |
| `gameobject` | Create, rename, move, delete, activate, deactivate GameObjects |
| `component` | Add, remove, and edit Component properties with serialized paths |
| `exec` | Execute C# expression in Unity |
| `log` | Query flight recorder |
| `session` | Manage execution sessions |
| `watch` | Stream Unity events in real-time |
| `scene` | Snapshot, diff, and save scene state |
| `schema` | Output machine-readable command schema |
| `workflow` | Run JSON workflow files |
| `tools` | List available commands with metadata |

## Status Codes

| Code | Name | Meaning |
|------|------|---------|
| 0 | Ready | Success |
| 100-103 | Transient | Unity is busy (auto-retry) |
| 104 | Accepted | Async operation started |
| 200 | NotFound | Unity not installed |
| 201 | ProjectLocked | Editor has project open (batch) |
| 203 | PluginNotInstalled | Run `init` first |
| 500+ | Error | Check logs |

## Testing

```bash
dotnet test unityctl.slnx                                            # All 312 tests
dotnet test unityctl.slnx --filter "FullyQualifiedName!~Integration" # Unit only

```

## Platforms

| Platform | CLI | IPC | Batch | CI |
|----------|-----|-----|-------|----|
| Windows | ✅ | Named Pipe | ✅ | ✅ |
| macOS | ✅ | Unix Domain Socket | ✅ | ✅ |
| Linux | ✅ | Unix Domain Socket | ✅ | ✅ |

## License

MIT
