# Getting Started with unityctl

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Unity 2021.3+](https://unity.com/download)

## Installation

### Option A: NuGet (recommended)

```bash
dotnet tool install -g unityctl
dotnet tool install -g unityctl-mcp   # MCP server (optional)
```

### Option B: Build from source

```bash
git clone https://github.com/kimjuyoung1127/unityctl.git
cd unityctl
dotnet build unityctl.slnx
```

> When building from source, replace `unityctl` with `dotnet run --project src/Unityctl.Cli --` in all examples below.

## Quick Start

### 1. Install the plugin into your Unity project

```bash
unityctl init --project /path/to/unity/project
```

This adds the `com.unityctl.bridge` UPM package to your project's `Packages/manifest.json`. Open (or restart) the Unity Editor after running this command.

### 2. Verify connectivity

```bash
# List installed Unity editors
unityctl editor list

# Ping the running Editor (IPC) or fall back to batchmode
unityctl ping --project /path/to/project --json

# Get editor state
unityctl status --project /path/to/project --json
```

### 3. Check compilation

```bash
unityctl check --project /path/to/project --json
```

Works headless — no Editor window required (uses batchmode fallback).

### 4. Run tests

```bash
# EditMode tests (waits for completion by default)
unityctl test --project /path/to/project --mode edit --json

# Fire-and-forget (returns immediately)
unityctl test --project /path/to/project --no-wait

# Custom timeout
unityctl test --project /path/to/project --timeout 60 --json
```

### 5. Build

```bash
# Build for Windows
unityctl build --project /path/to/project --target StandaloneWindows64 --json

# Preflight validation (no actual build)
unityctl build --project /path/to/project --dry-run --json
```

### 6. Diagnose issues

```bash
unityctl doctor --project /path/to/project --json
```

`doctor` checks IPC connectivity, plugin health, Editor log errors, and build state — useful as a first step when something fails.

## Common Workflows

### Scene & GameObject

```bash
# List scene hierarchy
unityctl scene hierarchy --project /path/to/project --json

# Create a GameObject
unityctl gameobject create --project /path/to/project --name "Player" --json

# Add a component
unityctl component add --project /path/to/project --target "Player" --component "Rigidbody" --json

# Save the scene
unityctl scene save --project /path/to/project --json
```

### Assets

```bash
# Search assets by type
unityctl asset find --project /path/to/project --filter "t:Material" --json

# Get asset info
unityctl asset get-info --project /path/to/project --path "Assets/Materials/Ground.mat" --json

# View dependency graph
unityctl asset reference-graph --project /path/to/project --path "Assets/Prefabs/Player.prefab" --json
```

### Script Management

```bash
# Create a new C# script
unityctl script create --project /path/to/project --name "PlayerController" --json

# List scripts
unityctl script list --project /path/to/project --folder Assets --json

# Validate compilation after edits
unityctl script validate --project /path/to/project --json
```

### Play Mode Control

```bash
unityctl play start --project /path/to/project --json
unityctl play pause --project /path/to/project --json
unityctl play stop  --project /path/to/project --json
```

### Real-time Monitoring

```bash
# Stream console logs
unityctl watch --project /path/to/project --channel console

# Stream hierarchy changes
unityctl watch --project /path/to/project --channel hierarchy
```

### Batch Edit with Rollback

```bash
unityctl batch execute --project /path/to/project --file ./batch.json --json
```

Executes multiple commands in a single IPC round-trip. If any step fails, all completed steps are rolled back automatically.

### Undo / Redo

```bash
unityctl undo --project /path/to/project --json
unityctl redo --project /path/to/project --json
```

All write commands register with Unity's Undo system.

## Architecture

```
unityctl.slnx
├── src/Unityctl.Shared   (netstandard2.1)  Protocol + models + constants
├── src/Unityctl.Core     (net10.0)         Business logic (transport, discovery, retry)
├── src/Unityctl.Cli      (net10.0)         CLI shell → dotnet tool "unityctl"
├── src/Unityctl.Mcp      (net10.0)         MCP server → dotnet tool "unityctl-mcp"
├── src/Unityctl.Plugin   (Unity UPM)       Editor bridge (IPC server)
└── tests/*                                 538+ xUnit tests
```

**Dependency direction**: `Shared ← Core ← Cli / Mcp`. Plugin runs inside Unity and shares source files with Shared.

## Transport

unityctl auto-selects the best available transport:

1. **IPC** (Named Pipe on Windows, Unix Domain Socket on macOS/Linux) — connects to a running Editor with the plugin installed. Typical latency ~100ms.
2. **Batch** — spawns Unity in batchmode when no Editor is running. Takes 30-120s but works headless in CI/CD.

The `CommandExecutor` probes IPC first and falls back to batch automatically.

## JSON Output

All commands support `--json` for machine-readable output:

```bash
unityctl status --project /path/to/project --json
unityctl asset find --project /path/to/project --filter "t:Prefab" --json
```

## StatusCode Reference

| Code | Name | Meaning | Action |
|------|------|---------|--------|
| 0 | Ready | Success | Done |
| 100-103 | Transient | Unity is busy (compiling, loading) | Retry automatically |
| 104 | Accepted | Async operation started | Poll for result |
| 200 | NotFound | No Unity editor found | Install Unity |
| 201 | ProjectLocked | Project locked by another process | Close other Editor or use IPC |
| 203 | PluginNotInstalled | Bridge plugin missing | Run `unityctl init` |
| 500+ | Error | Internal error | Check `doctor` output and Unity logs |

## Error Recovery

If a command fails:

1. `unityctl editor list` — is Unity installed?
2. `unityctl init --project <path>` — is the plugin installed?
3. `unityctl ping --project <path>` — is the Editor reachable?
4. `unityctl doctor --project <path> --json` — diagnose IPC, plugin, and Editor state
5. Check the Unity Editor log path shown in error output

## Running Tests (for contributors)

```bash
# All tests
dotnet test unityctl.slnx

# Unit tests only (no Unity required)
dotnet test unityctl.slnx --filter "FullyQualifiedName!~Integration"

# Specific project
dotnet test tests/Unityctl.Core.Tests
```

Integration tests require a runnable CLI executable and skip gracefully on restricted environments.
