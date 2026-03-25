# AI Agent Quickstart

This guide is for AI coding agents (Claude, Copilot, etc.) that need to automate Unity projects via unityctl.

## Setup

### 1. Install

```bash
dotnet tool install -g unityctl
dotnet tool install -g unityctl-mcp   # MCP server
```

`unityctl init` installs an embedded project-local bridge copy by default. Use `--source` only when you intentionally want a local path or Git-backed source install.

### 2. Install plugin into Unity project

```bash
unityctl init --project "/path/to/unity/project"
```

Open (or restart) the Unity Editor after running this command. For contributor workflows, explicit source installs are still supported:

```bash
unityctl init --project "/path/to/unity/project" --source /path/to/unityctl/src/Unityctl.Plugin
unityctl init --project "/path/to/unity/project" --source "https://github.com/Jason-hub-star/unityctl.git?path=/src/Unityctl.Plugin#v0.3.4"
```

### 3. Verify

```bash
unityctl editor list --json
unityctl ping --project "/path/to/project" --json
```

For first-run validation, prefer an already-open Editor. `ping` and `status` can fall back to batch mode, but that path is slower and may fail on a given project.

## MCP Server Setup

### Claude Code

Add to your MCP configuration:

```json
{
  "mcpServers": {
    "unityctl": {
      "command": "unityctl-mcp",
      "args": []
    }
  }
}
```

### Cursor / VS Code

Add to your MCP settings:

```json
{
  "mcpServers": {
    "unityctl": {
      "command": "unityctl-mcp"
    }
  }
}
```

The MCP server currently exposes 12 top-level tools, including `unityctl_query`, `unityctl_run`, `unityctl_schema`, `unityctl_status`, and `unityctl_watch`.

## Graph-Guided Review

For medium or cross-module work inside the `unityctl` repository, prefer a graph-first review pass before broad file reads.

Windows-first workflow:

```powershell
.\scripts\code_review_graph_report.ps1
.\scripts\code_review_graph_report.ps1 -Update
.\scripts\code_review_graph_report.ps1 -RebuildIfMissing
```

Notes:

- if `code-review-graph` is not installed directly, the script falls back to `uvx --from code-review-graph`
- the current graph surface for `unityctl` is effectively authored C# plus tests
- canonical docs remain source-of-truth reading material, but they are not expected to appear in the graph DB on this setup
- tuning and interpretation guidance lives in `docs/ref/CODE-REVIEW-GRAPH-TUNING.md`

## Tool Discovery

```bash
# Human-readable list
unityctl tools

# Machine-readable JSON (all 155 commands with parameter schemas)
unityctl tools --json

# Full CLI schema
unityctl schema

# Specific command schema over MCP
# unityctl_schema(command="gameobject-create")
```

AI agents should call `unityctl tools --json` or the MCP `unityctl_schema` tool to dynamically discover available commands and their parameters.
For UI inspection, prefer `ui find`/`ui get` over generic `gameobject find/get` when you specifically need Canvas ancestry, RectTransform data, or control state. For UI state changes, `ui toggle` and `ui input` set `Toggle.isOn` and `InputField.text` deterministically; they do not emulate clicks or keystrokes yet.
For CLI routing, `editor select` pins a project locally so `ping`, `status`, `check`, and `doctor` can omit `--project`.
When you already know a running Unity PID, `editor select --pid <pid>` can pin that process only when it maps to a single project path.
Use `editor instances` when you need visibility into currently running Unity processes before pinning or diagnosing routing.
For small artifact-first verification bundles, `workflow verify` now supports `projectValidate`, `capture`, `imageDiff`, `consoleWatch`, `uiAssert`, and `playSmoke`.
For UGUI interactions beyond toggle/input, use `ui scroll`, `ui slider-set`, `ui dropdown-set` to set ScrollRect/Slider/Dropdown values deterministically.
For URP/HDRP projects, `volume list/get/get-overrides/set-override` and `renderer-feature list` inspect and modify Volume overrides via Reflection (no hard dependency on URP/HDRP).
For Cinemachine, `cinemachine list/get/set-property` supports both 2.x and 3.x with runtime auto-detection.
For UI Toolkit, `uitk find/get/set-value` queries and modifies UIDocument elements at runtime.
For animation workflows, `animation list-clips/get-clip/get-controller/add-curve` inspect and edit AnimationClips and AnimatorControllers.
For profiling, `profiler get-stats/start/stop` provides memory and performance statistics (full rendering stats require Play Mode).

## Common Workflows

### Read project state

```bash
unityctl status --project "/path/to/project" --json
unityctl check --project "/path/to/project" --json
unityctl scene hierarchy --project "/path/to/project" --json
```

### Search and inspect

```bash
# Find assets
unityctl asset find --project "/path/to/project" --filter "t:Prefab" --json

# Find GameObjects
unityctl gameobject find --project "/path/to/project" --name "Player" --json

# Find UI controls
unityctl ui find --project "/path/to/project" --type Button --json

# Read one UI element
unityctl ui get --project "/path/to/project" --id "<GlobalObjectId>" --json

# Set a Toggle value deterministically
unityctl ui toggle --project "/path/to/project" --id "<GlobalObjectId>" --value true --mode auto --json

# Set InputField text deterministically
unityctl ui input --project "/path/to/project" --id "<GlobalObjectId>" --text "Alpha Beta" --mode auto --json

# Get component properties
unityctl component get --project "/path/to/project" --componentId "<ComponentId>" --json

# View dependency graph
unityctl asset reference-graph --project "/path/to/project" --path "Assets/Prefabs/Player.prefab" --json

# List cameras in loaded scenes
unityctl camera list --project "/path/to/project" --json

# Get camera details
unityctl camera get --project "/path/to/project" --id "<GlobalObjectId>" --json

# Find ScriptableObject assets
unityctl scriptableobject find --project "/path/to/project" --type "GameConfig" --json

# Get ScriptableObject properties
unityctl scriptableobject get --project "/path/to/project" --path "Assets/Data/config.asset" --json

# Find shaders
unityctl shader find --project "/path/to/project" --filter "Standard" --json

# Get shader properties
unityctl shader get-properties --project "/path/to/project" --name "Standard" --json

# Get texture import settings
unityctl texture get-import-settings --project "/path/to/project" --path "Assets/Textures/icon.png" --json

# Get model import settings
unityctl model get-import-settings --project "/path/to/project" --path "Assets/Models/character.fbx" --json

# Get audio import settings
unityctl audio get-import-settings --project "/path/to/project" --path "Assets/Audio/bgm.wav" --json

# List animation clips
unityctl animation list-clips --project "/path/to/project" --json

# Get animation clip details (curves, events)
unityctl animation get-clip --project "/path/to/project" --path "Assets/Animations/walk.anim" --json

# Get animator controller structure
unityctl animation get-controller --project "/path/to/project" --path "Assets/Animations/Player.controller" --json

# Profiler stats
unityctl profiler get-stats --project "/path/to/project" --json

# List URP/HDRP volumes
unityctl volume list --project "/path/to/project" --json

# List Cinemachine cameras
unityctl cinemachine list --project "/path/to/project" --json

# Find UI Toolkit elements
unityctl uitk find --project "/path/to/project" --type Button --json
```

### Create and modify

```bash
# Create a GameObject
unityctl gameobject create --project "/path/to/project" --name "Enemy" --json

# Add a component
unityctl component add --project "/path/to/project" --id "<GameObjectId>" --type "BoxCollider" --json

# Set a component property
unityctl component set-property --project "/path/to/project" --componentId "<ComponentId>" --property "m_Size" --value "[2,2,2]" --json

# Create a primitive mesh
unityctl mesh create-primitive --project "/path/to/project" --type Cube --name "EnemyBlockout" --position "[0,1,0]" --json

# Save scene
unityctl scene save --project "/path/to/project" --json
```

For quick blockouts or test geometry, `mesh create-primitive` creates Unity built-in primitives (`Cube`, `Sphere`, `Plane`, `Cylinder`, `Capsule`, `Quad`) and returns the created scene object metadata. This is a deterministic scene edit, not a mesh modeling workflow.

### Prefab instantiate

```bash
# Place a prefab into the scene
unityctl prefab instantiate --project "/path/to/project" --path "Assets/Prefabs/Enemy.prefab" --name "Enemy_1" --position "[5,0,3]" --json

# Place under a parent
unityctl prefab instantiate --project "/path/to/project" --path "Assets/Prefabs/Prop.prefab" --parent "<GlobalObjectId>" --json
```

For asset-backed scene dressing, `prefab instantiate` places prefab assets into the scene as proper prefab instances (maintaining the prefab link). Supports position, rotation, scale, and parent binding. Undo-backed. Replaces the previous `exec` workaround.

`asset copy` now also accepts external absolute paths (outside the Unity project), enabling staged asset import workflows without shell copy workarounds.

### Script management

```bash
# Create a new script
unityctl script create --project "/path/to/project" --path "Assets/Scripts/EnemyAI.cs" --className "EnemyAI" --json

# Edit a script (whole-file replace)
unityctl script edit --project "/path/to/project" --path "Assets/Scripts/EnemyAI.cs" --contentFile ./EnemyAI.cs --json

# Validate compilation
unityctl script validate --project "/path/to/project" --json

# List scripts
unityctl script list --project "/path/to/project" --folder Assets --json
```

`script get-errors`, `script find-refs`, and `script rename-symbol` are best used with a running Editor and IPC ready. If `script get-errors` returns no compile data after Unity reports Ready, run `unityctl script validate --project "/path/to/project" --wait` once and retry.

### Build and test

```bash
# Preflight validation
unityctl build --project "/path/to/project" --dry-run --json

# Build
unityctl build --project "/path/to/project" --target StandaloneWindows64 --json

# Run tests
unityctl test --project "/path/to/project" --mode edit --json

# Start PlayMode tests asynchronously
unityctl test --project "/path/to/project" --mode play --no-wait --json

# Poll an async test run
unityctl test-result --project "/path/to/project" --request-id "<requestId>" --json
```

`test --filter` currently maps directly to Unity Test Runner `Filter.testNames` and should be treated as an exact-match value, not a fuzzy substring search.
For PlayMode, `test --wait` is not a true synchronous path yet; use the returned `requestId` with `test-result`.

### Batch edit with rollback

```bash
unityctl batch execute --project "/path/to/project" --file ./batch.json --json
```

Sends multiple commands in one IPC round-trip. If any step fails, completed steps are rolled back automatically.

Rollback coverage:
- **Undo-backed**: `gameobject-*`, `component-*`, `ui-*`, `material-set`, `material-set-shader`, `player-settings`, `project-settings set`, `prefab unpack`
- **Compensation-backed**: `asset-create`, `asset-copy`, `asset-move`

### Scene diff

```bash
# Take a snapshot
unityctl scene snapshot --project "/path/to/project" --json

# Compare with live state
unityctl scene diff --project "/path/to/project" --live --json
```

### Screenshot (MCP)

Screenshot capture is exposed through `unityctl_query` with `command="screenshot"`, returning Scene or Game View as base64 PNG/JPG for visual verification workflows.

## StatusCode Reference

| Code | Name | Meaning | Action |
|------|------|---------|--------|
| 0 | Ready | Success | Done |
| 100-103 | Transient | Unity is busy | Retry automatically |
| 104 | Accepted | Async operation started | Poll for result |
| 200 | NotFound | No Unity installed | Install Unity |
| 201 | ProjectLocked | Project locked | Close Editor or use IPC |
| 203 | PluginNotInstalled | Plugin missing | Run `unityctl init` |
| 500+ | Error | Internal error | Check `doctor` output |

## Transport

unityctl auto-selects transport:

1. **IPC** — connects to a running Editor via Named Pipe (Windows) or Unix Domain Socket (macOS/Linux). This is the best path for first-run validation.
2. **Batch** — spawns Unity in batchmode. It can support headless workflows, but startup latency is much higher and project-specific failures still happen in practice.

## Error Recovery

If a command fails:

1. `unityctl doctor --project <path> --json` — diagnose IPC, plugin, recent failures, and recovery steps
2. `unityctl ping --project <path>` — verify Editor connectivity
3. `unityctl init --project <path>` — reinstall plugin if missing
4. Check the Unity Editor log path shown in error output

`doctor` now includes the configured plugin source kind (`embedded`, `local-file`, `git`), bridge enabled state, embedded path when relevant, project lock detection, recent failure summaries, active session hints, and recommended next actions.
When IPC is healthy, a detected lockfile is informational rather than an automatic error by itself.
For recent script failures, `doctor` also distinguishes compile/reload waiting from script compile-cache issues and recommends either `status --wait` or `script validate --wait` accordingly.
