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
- `unityctl init` now installs an embedded `Packages/com.unityctl.bridge` copy by default.
- Use `unityctl init --source <path-or-git-url>` only when you intentionally want a contributor-style local or Git-backed package reference.
- GitHub Release CLI archives are framework-dependent publishes (`self-contained false`), not self-contained single-file binaries.
- The validated Apple silicon macOS path is Homebrew `.NET 10` + `dotnet tool install -g unityctl` + Unity Hub.

### Option B: Build from source

```bash
git clone https://github.com/Jason-hub-star/unityctl.git
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
unityctl init --project /path/to/unity/project
```

By default this copies a bundled `com.unityctl.bridge` package into `Packages/com.unityctl.bridge` and writes `ProjectSettings/UnityctlSettings.asset` so the bridge only auto-starts for projects explicitly installed via `unityctl`.

If you need a contributor-style source install instead, you can still do one of these explicitly:

```bash
unityctl init --project /path/to/unity/project --source /path/to/unityctl/src/Unityctl.Plugin
unityctl init --project /path/to/unity/project --source "https://github.com/Jason-hub-star/unityctl.git?path=/src/Unityctl.Plugin#v0.3.4"
```

When switching away from an older `file:` install, first run:

```bash
unityctl detach --project /path/to/unity/project --clean-cache
unityctl init --project /path/to/unity/project
```

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

# Start PlayMode tests asynchronously
unityctl test --project /path/to/project --mode play --no-wait --json

# Poll an async test run
unityctl test-result --project /path/to/project --request-id "<requestId>" --json

# Custom timeout
unityctl test --project /path/to/project --timeout 60 --json
```

`test --filter` currently maps to Unity Test Runner `Filter.testNames` and should be treated as an exact-match filter value.
For PlayMode, prefer `--no-wait` plus `test-result` polling rather than assuming a blocking `--wait` flow.

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
It also reports the configured plugin source kind (`embedded`, `local-file`, `git`), bridge enabled state, embedded path when relevant, active session hints, recommended next steps, and whether a Unity project lock is currently detected.
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

### Prefab instantiate

```bash
# Place a prefab into the scene with position and name
unityctl prefab instantiate --project /path/to/project --path "Assets/Prefabs/Enemy.prefab" --name "Enemy_1" --position "[5,0,3]" --json

# Place under a parent GameObject
unityctl prefab instantiate --project /path/to/project --path "Assets/Prefabs/Prop.prefab" --parent "<GlobalObjectId>" --json
```

`prefab instantiate` places a prefab asset into the active scene as a prefab instance (maintaining the prefab link). Supports position, rotation, scale, and parent. Undo-backed.

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

```bash
# Set a ScrollRect position
unityctl ui scroll --project /path/to/project --id "<GlobalObjectId>" --x 0.5 --y 1.0 --json

# Set a Slider value
unityctl ui slider-set --project /path/to/project --id "<GlobalObjectId>" --value 0.75 --json

# Set a Dropdown index
unityctl ui dropdown-set --project /path/to/project --id "<GlobalObjectId>" --value 2 --json
```

### Animation

```bash
# List animation clips
unityctl animation list-clips --project /path/to/project --json

# Get clip details (curves, events, length)
unityctl animation get-clip --project /path/to/project --path "Assets/Animations/walk.anim" --json

# Get animator controller structure (layers, states, transitions)
unityctl animation get-controller --project /path/to/project --path "Assets/Animations/Player.controller" --json

# Add a curve to a clip
unityctl animation add-curve --project /path/to/project --path "Assets/Animations/walk.anim" --binding '{"path":"","type":"UnityEngine.Transform","propertyName":"m_LocalPosition.x"}' --keys '[{"time":0,"value":0},{"time":1,"value":1}]' --json
```

### Import Settings

```bash
# Model import settings (FBX, OBJ, etc.)
unityctl model get-import-settings --project /path/to/project --path "Assets/Models/character.fbx" --json

# Audio import settings
unityctl audio get-import-settings --project /path/to/project --path "Assets/Audio/bgm.wav" --json

# Export assets as .unitypackage
unityctl asset export --project /path/to/project --paths "Assets/Prefabs/Player.prefab" --output "C:/temp/export.unitypackage" --json
```

### Profiler

```bash
# Get memory/performance stats (full rendering stats require Play Mode)
unityctl profiler get-stats --project /path/to/project --json

# Enable/disable profiler
unityctl profiler start --project /path/to/project --json
unityctl profiler stop --project /path/to/project --json
```

### URP/HDRP Volume (requires URP or HDRP)

```bash
# List Volume components in the scene
unityctl volume list --project /path/to/project --json

# Get Volume details and overrides
unityctl volume get --project /path/to/project --id "<GlobalObjectId>" --json

# Get all parameters of a VolumeComponent
unityctl volume get-overrides --project /path/to/project --id "<GlobalObjectId>" --component Bloom --json

# Set a Volume override parameter
unityctl volume set-override --project /path/to/project --id "<GlobalObjectId>" --component Bloom --property intensity --value 0.5 --json
```

### Cinemachine (requires Cinemachine package)

```bash
# List virtual cameras (auto-detects 2.x vs 3.x)
unityctl cinemachine list --project /path/to/project --json

# Get virtual camera details
unityctl cinemachine get --project /path/to/project --id "<GlobalObjectId>" --json

# Set a camera property
unityctl cinemachine set-property --project /path/to/project --id "<GlobalObjectId>" --property "m_Lens.FieldOfView" --value 60 --json
```

### UI Toolkit (requires Unity 2021.2+)

```bash
# Find UI Toolkit elements
unityctl uitk find --project /path/to/project --type Button --json

# Get element details
unityctl uitk get --project /path/to/project --name "myButton" --json

# Set element value
unityctl uitk set-value --project /path/to/project --name "myTextField" --value "hello" --json
```

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
