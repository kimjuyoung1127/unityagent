# AI Agent Quickstart

This guide is for AI coding agents (Claude, Copilot, etc.) that need to automate Unity projects via unityctl.

## Setup

### 1. Install

```bash
dotnet tool install -g unityctl
dotnet tool install -g unityctl-mcp   # MCP server
```

Today the CLI packages do not bundle the Unity plugin directly. `unityctl init` supports either a local `Unityctl.Plugin` checkout or an explicit Git URL source.

### 2. Install plugin into Unity project

```bash
unityctl init --project "/path/to/unity/project" --source "https://github.com/kimjuyoung1127/unityctl.git?path=/src/Unityctl.Plugin#v0.2.0"
```

Open (or restart) the Unity Editor after running this command. When you run from a cloned `unityctl` workspace, the CLI can usually find `src/Unityctl.Plugin` without `--source`; you can also pass that local path explicitly instead of a Git URL.

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

## Tool Discovery

```bash
# Human-readable list
unityctl tools

# Machine-readable JSON (all 131 commands with parameter schemas)
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
```

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

`doctor` now includes the configured plugin source (`file:` vs Git URL), project lock detection, recent failure summaries, active session hints, and recommended next actions.
When IPC is healthy, a detected lockfile is informational rather than an automatic error by itself.
For recent script failures, `doctor` also distinguishes compile/reload waiting from script compile-cache issues and recommends either `status --wait` or `script validate --wait` accordingly.
