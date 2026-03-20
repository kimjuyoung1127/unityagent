# Getting Started with unityctl

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Unity 2021.3+](https://unity.com/download)
- macOS is validated on Apple silicon with Homebrew + Unity Hub

## Installation

### Option A: NuGet (recommended)

```bash
dotnet tool install -g unityctl
dotnet tool install -g unityctl-mcp   # MCP server (optional)
```

Important today:

- `dotnet tool install` gives you the CLI and MCP entrypoints.
- `unityctl init` supports either a local `Unityctl.Plugin` source checkout or an explicit Git URL source. When `--source` is omitted, it still falls back to local workspace discovery.
- GitHub Release CLI archives are framework-dependent publishes (`self-contained false`), not self-contained single-file binaries.
- The validated Apple silicon macOS path is Homebrew `.NET 10` + `dotnet tool install -g unityctl` + Unity Hub.

### Option B: Build from source

```bash
git clone https://github.com/kimjuyoung1127/unityctl.git
cd unityctl
dotnet build unityctl.slnx
```

> When building from source, replace `unityctl` with `dotnet run --project src/Unityctl.Cli --` in all examples below.
>
> The 2026-03-19 macOS smoke test used the published `dotnet tool` install path plus a local plugin checkout. Treat source-build validation as a separate contributor workflow.

## Apple Silicon macOS Smoke Test

Manual validation was completed on an Apple silicon MacBook Air with:

- Homebrew
- `.NET SDK 10.0.105`
- Unity Hub
- Unity `6000.0.64f1`
- Unity `6000.3.11f1`

Verified commands:

```bash
unityctl editor list --json
unityctl init --project /path/to/project --source /path/to/unityctl/src/Unityctl.Plugin
unityctl ping --project /path/to/project --json
unityctl doctor --project /path/to/project --json
unityctl status --project /path/to/project --json
unityctl check --project /path/to/project --json
```

Observed result on the Unity `6000.0.64f1` validation project:

- `ping` returned `pong`
- `doctor` reported IPC connected
- `status` returned `Ready`
- `check` reported `Compilation check passed`

Important caveat: one validation project depended on a third-party Unity package that explicitly supports Unity `6.0 LTS` only. Opening that project in `6000.3.11f1` produced a project-side render pipeline failure, while reopening it in `6000.0.64f1` removed the error. That issue was project/version compatibility, not a macOS-specific `unityctl` transport failure.

## Quick Start

### 1. Install the plugin into your Unity project

```bash
unityctl init --project /path/to/unity/project --source "https://github.com/kimjuyoung1127/unityctl.git?path=/src/Unityctl.Plugin#v0.2.0"
```

This adds the `com.unityctl.bridge` UPM package to your project's `Packages/manifest.json`. With a local path it writes a `file:` package reference; with a Git URL it writes the URL directly. If you run `unityctl` from a cloned `unityctl` workspace, `--source` can still be omitted because the CLI will try to find `src/Unityctl.Plugin` automatically. Open (or restart) the Unity Editor after running this command.

### 2. Verify connectivity

```bash
# List installed Unity editors
unityctl editor list

# Inspect running Unity instances (PID / project / IPC)
unityctl editor instances --json

# Optionally pin the current project for project-less CLI checks
unityctl editor select --project /path/to/project

# Or pin a running Unity PID when it uniquely maps to one project
unityctl editor select --pid 55028

# Ping the running Editor (IPC) or fall back to batchmode
unityctl ping --project /path/to/project --json

# After selection, ping/status/check/doctor can omit --project
unityctl ping --json

# Get editor state
unityctl status --project /path/to/project --json
```

For onboarding, prefer verifying with a running Editor. In batch fallback these commands can take tens of seconds or fail on a specific project, so they are not a guaranteed sub-minute first-success path yet.

### 3. Check compilation

```bash
unityctl check --project /path/to/project --json
```

Can run headless via batchmode fallback, but the result remains project-dependent and startup latency is much higher than IPC.

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

`doctor` checks IPC connectivity, plugin health, Editor log errors, build state, and recent project-specific failures — useful as a first step when something fails.
It also reports the configured plugin source, active session hints, recommended next steps, and whether a Unity project lock is currently detected.
When IPC is already healthy, a detected Unity lockfile is treated as informational rather than an automatic failure signal.

### 7. Run a verification bundle

```bash
unityctl workflow verify --file ./verify.json --project /path/to/project --json
```

`workflow verify` is the first Visual Verification v2 slice. Today it supports `projectValidate`, `capture`, `imageDiff`, `consoleWatch`, `uiAssert`, and `playSmoke`, and writes artifact-first output under `~/.unityctl/verification/`.

## Common Workflows

### Scene & GameObject

```bash
# List scene hierarchy
unityctl scene hierarchy --project /path/to/project --json

# Create a GameObject
unityctl gameobject create --project /path/to/project --name "Player" --json

# Add a component
unityctl component add --project /path/to/project --id "<PlayerId>" --type "Rigidbody" --json

# Create a primitive mesh
unityctl mesh create-primitive --project /path/to/project --type Cube --name "FloorBlock" --position "[0,0,0]" --json

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

### Mesh primitives

```bash
# Create a Cube primitive
unityctl mesh create-primitive --project /path/to/project --type Cube --name "DebugCube" --position "[1,2,3]" --scale "[2,1,2]" --json
```

`mesh create-primitive` currently targets Unity's built-in primitive set (`Cube`, `Sphere`, `Plane`, `Cylinder`, `Capsule`, `Quad`). Like other scene write commands, it is most reliable with a running Editor and IPC ready.

### Camera

```bash
# List all cameras in loaded scenes
unityctl camera list --project /path/to/project --json

# Get detailed camera properties
unityctl camera get --project /path/to/project --id "<GlobalObjectId>" --json
```

### Texture import

```bash
# Read texture import settings
unityctl texture get-import-settings --project /path/to/project --path "Assets/Textures/icon.png" --json

# Change a texture import setting (triggers reimport)
unityctl texture set-import-settings --project /path/to/project --path "Assets/Textures/icon.png" --property maxTextureSize --value 512 --json
```

### ScriptableObject

```bash
# Find ScriptableObject assets
unityctl scriptableobject find --project /path/to/project --type "GameConfig" --json

# Get serialized properties
unityctl scriptableobject get --project /path/to/project --path "Assets/Data/config.asset" --json

# Set a property (Undo-backed)
unityctl scriptableobject set-property --project /path/to/project --path "Assets/Data/config.asset" --property "m_Name" --value "newName" --json
```

### Shader

```bash
# Find shaders in the project
unityctl shader find --project /path/to/project --filter "Standard" --json

# Get shader properties
unityctl shader get-properties --project /path/to/project --name "Standard" --json
```

### UI inspection

```bash
# Find UI buttons on loaded Canvases
unityctl ui find --project /path/to/project --type Button --limit 10 --json

# Read RectTransform + UI component state for one element
unityctl ui get --project /path/to/project --id "<GlobalObjectId>" --json

# Set a Toggle state deterministically (not a click simulation)
unityctl ui toggle --project /path/to/project --id "<GlobalObjectId>" --value true --mode auto --json

# Set an InputField text deterministically (not keystroke simulation)
unityctl ui input --project /path/to/project --id "<GlobalObjectId>" --text "Alpha Beta" --mode auto --json
```

`ui find`/`ui get` are currently UGUI-first (`Canvas`, `RectTransform`, `Selectable`, `Text`, `InputField`, `Toggle`, `Slider`, `Dropdown`, `ScrollRect`). `ui toggle` and `ui input` extend that slice with deterministic state changes for `Toggle.isOn` and `InputField.text`. These are not real click or typing simulation yet, and in practice they are most reliable with a running Editor and IPC ready.

### Script Management

```bash
# Create a new C# script
unityctl script create --project /path/to/project --path "Assets/Scripts/PlayerController.cs" --className "PlayerController" --json

# List scripts
unityctl script list --project /path/to/project --folder Assets --json

# Validate compilation after edits
unityctl script validate --project /path/to/project --json
```

For `script get-errors`, `script find-refs`, and `script rename-symbol`, prefer a running Editor with IPC ready. If `script get-errors` still has no compile data after Unity reports Ready, run `unityctl script validate --project <path> --wait` once and retry.

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
└── tests/*                                 689 xUnit tests
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
4. `unityctl doctor --project <path> --json` — diagnose IPC, plugin, recent failures, and suggested recovery steps
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
