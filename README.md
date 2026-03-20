# unityctl

[![NuGet](https://img.shields.io/nuget/v/unityctl?label=unityctl)](https://www.nuget.org/packages/unityctl)
[![NuGet](https://img.shields.io/nuget/v/unityctl-mcp?label=unityctl-mcp)](https://www.nuget.org/packages/unityctl-mcp)
[![CI](https://github.com/kimjuyoung1127/unityctl/actions/workflows/ci-dotnet.yml/badge.svg)](https://github.com/kimjuyoung1127/unityctl/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

### The execution layer for AI-driven game development.

Give your AI agent **131 commands** to build Unity scenes, write C# scripts, validate builds, and ship games — with automatic rollback when things go wrong.

```
131 CLI commands · 12 MCP tools · 689 tests · Windows / macOS / Linux
```

<p align="center">
  <img src="docs/assets/mcp-demo.svg" alt="AI agent building a Unity scene via MCP" width="700">
</p>

---

## The Problem

AI agents can write code, but they **can't build games** — because Unity has no programmatic interface for scene editing, asset management, or project validation.

Existing Unity MCP servers try to fix this, but they create new problems for AI agents:

| Pain Point | Impact on AI Agent |
|---|---|
| **45 KB+ schemas** loaded every turn | Wastes tokens on tool definitions instead of reasoning |
| **No validation feedback** | Agent can't tell if the scene is broken after changes |
| **No rollback** | One bad command corrupts the project state |
| **WebSocket drops on Play Mode** | Agent loses connection during Unity's Domain Reload |
| **Editor must be open** | CI/CD pipelines can't run without a GUI |

## The Solution

unityctl is a **.NET CLI + MCP server** that turns Unity Editor into a programmable API.

For AI agents, this means a **closed-loop automation cycle** — the agent doesn't just _execute_ commands, it can _verify_ results, _diagnose_ failures, and _recover_ from mistakes:

<p align="center">
  <img src="docs/assets/agent-loop.svg" alt="Plan - Execute - Verify - Diagnose Loop" width="680">
</p>

> **Other tools give agents hands. unityctl gives agents hands, eyes, and a safety net.**

---

## What AI Agents Can Build

### Scene Construction

> _"Create a platformer level with a floor, walls, and a player spawn point"_

```bash
# Agent creates scene structure
unityctl scene create --path "Assets/Scenes/Level01.unity" --project $P
unityctl mesh create-primitive --type Plane --name "Floor" --scale "[10,1,10]" --project $P
unityctl mesh create-primitive --type Cube --name "Wall" --position "[5,1,0]" --scale "[0.5,2,10]" --project $P
unityctl gameobject create --name "PlayerSpawn" --project $P
unityctl component add --id "<PlayerSpawnId>" --type "BoxCollider" --project $P

# Agent verifies the scene
unityctl scene hierarchy --project $P --json      # check structure
unityctl screenshot capture --project $P           # visual verification
unityctl project validate --project $P --json      # camera? lights? errors?
```

### Script Authoring with Compile Verification

> _"Write a player movement script and make sure it compiles"_

```bash
# Agent writes code
unityctl script create --path "Assets/Scripts/PlayerMovement.cs" --className "PlayerMovement" --project $P
unityctl script patch --path "Assets/Scripts/PlayerMovement.cs" \
  --startLine 8 --insertContent "public float speed = 5f;" --project $P

# Agent checks compilation — and fixes errors in a loop
unityctl script validate --project $P --wait       # trigger recompile
unityctl script get-errors --project $P --json     # structured CS errors
# if errors: read error, patch fix, validate again
```

### Safe Batch Operations with Rollback

> _"Set up physics layers for Player, Enemy, and Projectile — roll back if anything fails"_

```bash
unityctl batch execute --project $P --rollbackOnFailure true --commands '[
  {"command": "layer-set", "parameters": {"index": 8, "name": "Player"}},
  {"command": "layer-set", "parameters": {"index": 9, "name": "Enemy"}},
  {"command": "layer-set", "parameters": {"index": 10, "name": "Projectile"}},
  {"command": "physics-set-collision-matrix", "parameters": {"layer1": 10, "layer2": 10, "ignore": true}}
]'
# If any command fails, all changes are automatically rolled back via Undo
```

### Build Verification Pipeline

> _"Check if the project is ready to ship"_

<p align="center">
  <img src="docs/assets/project-validate.svg" alt="project-validate output showing 6 checks" width="600">
</p>

```bash
# Agent reads the failure, fixes it, validates again
unityctl gameobject create --name "Main Camera" --project $P
unityctl component add --id "<MainCameraId>" --type "Camera" --project $P
unityctl gameobject set-tag --id "<MainCameraId>" --tag "MainCamera" --project $P
unityctl project validate --project $P --json   # valid: true
```

---

## Why unityctl for AI Agents?

| | unityctl | Existing Unity MCP |
|---|---|---|
| **Schema overhead** | **5 KB** per session (9x smaller) | 45 KB+ loaded every turn |
| **Validation loop** | `project validate` + `scene diff` + `screenshot capture` | Agent flies blind |
| **Error recovery** | `script get-errors` with file/line/column | Raw console output or nothing |
| **Safe experimentation** | `batch execute --rollbackOnFailure` + `undo` | No rollback — mistakes are permanent |
| **Connection stability** | Named Pipe — survives Domain Reload | WebSocket drops, reconnect needed |
| **CI/CD** | `check` / `test` / `build --dry-run` work headless | Editor must be open |
| **Diagnostics** | `doctor` classifies failures + suggests next steps | "Connection failed" |
| **Commands** | **131** (read + write + validate + diagnose) | ~34-200 tools |
| **Audit trail** | NDJSON flight recorder for every command | No history |
| **Runtime** | Native .NET — no Python/TS bridge | Bridge overhead |
| **Install** | `dotnet tool install -g unityctl` | Node.js + npm + port config |
| **License** | **MIT** | Varies |

### Token Efficiency

AI agent costs are dominated by tool schemas sent every turn. unityctl uses **on-demand schema loading**:

<p align="center">
  <img src="docs/assets/token-efficiency.svg" alt="83x less tokens per turn" width="600">
</p>

The 12 MCP tools cover the full 131-command surface through `unityctl_query` (read), `unityctl_run` (write), and `unityctl_schema` (lookup).

---

## Selection-aware Routing

```bash
# Pin the current Unity project once
unityctl editor select --project /path/to/project

# Or pin a running Unity PID when it maps to a single project
unityctl editor select --pid 55028

# Inspect the current selection
unityctl editor current --json

# See running Unity instances with PID / project / IPC status
unityctl editor instances --json

# These CLI commands can now omit --project
unityctl ping --json
unityctl status --json
unityctl check --json
unityctl doctor --json

# Run a small verification bundle (artifacts-first)
unityctl workflow verify --file verify.json --project /path/to/project --json
```

## Install

```bash
# CLI (requires .NET 10 SDK)
dotnet tool install -g unityctl

# MCP server for AI agents
dotnet tool install -g unityctl-mcp
```

Bootstrap notes:
- `--source` accepts a local `Unityctl.Plugin` folder or a Git URL: `https://github.com/kimjuyoung1127/unityctl.git?path=/src/Unityctl.Plugin#v0.2.0`
- GitHub Release CLI archives are framework-dependent (not self-contained) today.

### Apple Silicon macOS Validation

Manual validation was completed on an Apple silicon MacBook Air using Homebrew, .NET SDK `10.0.105`, Unity Hub, and Unity editors `6000.0.64f1` and `6000.3.11f1`.

Validated path:

- `dotnet tool install -g unityctl`
- `dotnet tool install -g unityctl-mcp`
- `unityctl editor list`
- `unityctl init --project <project> --source /path/to/unityctl/src/Unityctl.Plugin`
- `unityctl ping --project <project> --json`
- `unityctl doctor --project <project> --json`
- `unityctl status --project <project> --json`
- `unityctl check --project <project> --json`

Observed result on a Unity `6000.0.64f1` project: `ping` returned `pong`, `doctor` reported IPC connected, `status` returned `Ready`, and `check` passed on macOS.

Project compatibility note: if a Unity project or third-party package is pinned to Unity `6.0 LTS`, opening that same project in `6000.3+` can fail before `unityctl` is involved. During validation, reopening the project in its pinned `6000.0.64f1` editor resolved the project-side render pipeline error.

## Quick Start

```bash
# 1. Install the Editor plugin
unityctl init --project /path/to/project \
  --source "https://github.com/kimjuyoung1127/unityctl.git?path=/src/Unityctl.Plugin#v0.2.0"

# 2. Open the project in Unity Editor, then verify connectivity
unityctl ping --project /path/to/project --json
unityctl status --project /path/to/project --json

# 3. Start building
unityctl gameobject create --name "Player" --project /path/to/project
unityctl component add --id "<PlayerId>" --type "Rigidbody" --project /path/to/project
unityctl scene save --project /path/to/project

# 4. Validate
unityctl project validate --project /path/to/project --json

# 5. Build
unityctl build --project /path/to/project --dry-run    # 13 preflight checks
```

### MCP Setup (AI Agents)

Add to your Claude Code / Cursor / VS Code MCP config:

```json
{
  "mcpServers": {
    "unityctl": {
      "command": "unityctl-mcp"
    }
  }
}
```

<details>
<summary><strong>12 MCP Tools</strong></summary>

| Tool | Type | Description |
|------|------|-------------|
| `unityctl_query` | Read | Unified read: asset, gameobject, scene, component, UI, physics, lighting, tags |
| `unityctl_run` | Write | Unified write: create, delete, modify, script, material, prefab, batch |
| `unityctl_schema` | Meta | On-demand parameter lookup (by command or category) |
| `unityctl_build` | Action | Build player with 13 preflight checks |
| `unityctl_check` | Action | Compile verification (headless) |
| `unityctl_test` | Action | EditMode / PlayMode tests |
| `unityctl_exec` | Action | Execute arbitrary C# expression |
| `unityctl_status` | Read | Editor state + connectivity |
| `unityctl_ping` | Read | Fast connectivity check |
| `unityctl_watch` | Stream | Real-time console / hierarchy / compilation events |
| `unityctl_log` | Read | Flight recorder query |
| `unityctl_session_list` | Read | Active session list |

</details>

---

## Commands (131)

### Core (13)

| Command | Description |
|---------|-------------|
| `ping` | Check Unity connectivity |
| `status` | Editor state (with `--wait` smart polling for Domain Reload) |
| `check` | Verify script compilation (headless) |
| `build` | Build player with `--dry-run` preflight (13 checks) |
| `test` | Run EditMode / PlayMode tests |
| `doctor` | Diagnose connectivity + suggest recovery steps |
| `project validate` | Game readiness check (compile, scenes, camera, lights, console, editor) |
| `init` | Install plugin to Unity project |
| `editor list` | Discover installed Unity editors |
| `editor instances` | List running Unity Editor instances |
| `editor current` | Show the selected Unity project target |
| `editor select` | Select a Unity project target, or a unique running PID, for project-less CLI routing |
| `workflow verify` | Run artifact-first verification steps (`projectValidate`, `capture`, `imageDiff`, `consoleWatch`, `uiAssert`, `playSmoke`) |

<details>
<summary><strong>Scene & GameObject</strong> (19)</summary>

| Command | Description |
|---------|-------------|
| `scene snapshot` | Capture scene state |
| `scene hierarchy` | Scene hierarchy tree |
| `scene diff` | Property-level scene diff with epsilon |
| `scene save` | Save active scene |
| `scene open` | Open scene by path |
| `scene create` | Create new scene |
| `gameobject create` | Create GameObject |
| `gameobject delete` | Delete GameObject |
| `gameobject rename` | Rename GameObject |
| `gameobject move` | Reparent GameObject |
| `gameobject find` | Find by name/tag/component |
| `gameobject get` | Get GameObject details |
| `gameobject set-active` | Toggle active state |
| `gameobject set-tag` | Set tag |
| `gameobject set-layer` | Set layer |
| `component add` | Add component |
| `component remove` | Remove component |
| `component get` | Get component properties |
| `component set-property` | Set component property |

</details>

<details>
<summary><strong>Assets & Materials</strong> (21)</summary>

| Command | Description |
|---------|-------------|
| `asset find` | Search by type/label/path |
| `asset get-info` | Asset metadata |
| `asset get-dependencies` | Direct dependencies |
| `asset reference-graph` | Reverse-reference graph |
| `asset create` | Create asset |
| `asset create-folder` | Create folder |
| `asset copy` | Copy asset |
| `asset move` | Move/rename asset |
| `asset delete` | Delete asset |
| `asset import` | Reimport asset |
| `asset refresh` | Refresh AssetDatabase |
| `asset get-labels` | Get labels |
| `asset set-labels` | Set labels |
| `material create` | Create material |
| `material get` | Get material properties |
| `material set` | Set material property |
| `material set-shader` | Change shader |
| `prefab create` | Create prefab from GameObject |
| `prefab unpack` | Unpack prefab instance |
| `prefab apply` | Apply prefab overrides |
| `prefab edit` | Enter/exit prefab edit mode |

</details>

<details>
<summary><strong>Scripting & Code Analysis</strong> (10)</summary>

| Command | Description |
|---------|-------------|
| `script create` | Create C# script from template |
| `script edit` | Replace script content (whole-file) |
| `script patch` | Line-level insert/delete/replace |
| `script delete` | Delete script file |
| `script validate` | Trigger compilation and verify |
| `script list` | List MonoScript assets |
| `script get-errors` | Structured compile errors (file/line/column/code) |
| `script find-refs` | Find symbol references across all scripts |
| `script rename-symbol` | Rename symbol across all scripts (with `--dry-run`) |
| `exec` | Execute C# expression in Unity |

</details>

<details>
<summary><strong>Editor Control</strong> (18)</summary>

| Command | Description |
|---------|-------------|
| `play start/stop/pause` | Start, stop, or pause play mode |
| `editor pause` | Toggle editor pause |
| `editor focus-gameview` | Focus Game View |
| `editor focus-sceneview` | Focus Scene View |
| `player-settings get/set` | PlayerSettings read/write |
| `project-settings get/set` | Project settings read/write |
| `console clear` | Clear console |
| `console get-count` | Log/warning/error counts |
| `define-symbols get/set` | Scripting define symbols |
| `tag list/add` | Tag management |
| `layer list/set` | Layer management |
| `undo` | Undo last operation |
| `redo` | Redo last undone operation |

</details>

<details>
<summary><strong>Build & Deployment</strong> (6)</summary>

| Command | Description |
|---------|-------------|
| `build-profile list/get-active/set-active` | Build profile management |
| `build-target switch` | Switch build platform |
| `build-settings get-scenes/set-scenes` | Build scene list |

</details>

<details>
<summary><strong>Physics, Lighting & NavMesh</strong> (12)</summary>

| Command | Description |
|---------|-------------|
| `physics get-settings/set-settings` | DynamicsManager |
| `physics get-collision-matrix/set-collision-matrix` | 32x32 layer collision |
| `lighting bake/cancel/clear` | Lightmap baking |
| `lighting get-settings/set-settings` | Lightmap settings |
| `navmesh bake/clear/get-settings` | NavMesh |

</details>

<details>
<summary><strong>UI & Mesh</strong> (8)</summary>

| Command | Description |
|---------|-------------|
| `ui canvas-create` | Create UI Canvas |
| `ui element-create` | Create Button, Text, Image, etc. |
| `ui set-rect` | Set RectTransform |
| `ui find` | Find UI elements |
| `ui get` | Get UI element details |
| `ui toggle` | Set Toggle state |
| `ui input` | Set InputField text |
| `mesh create-primitive` | Create Cube/Sphere/Plane/Cylinder/Capsule/Quad |

</details>

<details>
<summary><strong>Automation & Monitoring</strong> (15)</summary>

| Command | Description |
|---------|-------------|
| `batch execute` | Transaction with rollback |
| `workflow run` | JSON workflow execution |
| `watch` | Real-time event streaming |
| `log` | Flight recorder query |
| `session list/stop/clean` | Session management |
| `screenshot` | Scene/Game View capture (base64) |
| `schema` / `tools` | Machine-readable metadata |
| `package list/add/remove` | Package management |
| `animation create-clip/create-controller` | Animation assets |

</details>

---

## Architecture

```
AI Agent (LLM)                unityctl-mcp              unityctl CLI             Unity Editor
Claude / GPT / Gemini         12 MCP tools              131 commands             Plugin (IPC)
        |                          |                          |                       |
        |--- MCP (stdio) -------->|                          |                       |
        |                          |--- CLI invocation ----->|                       |
        |                          |                          |--- IPC (~100ms) ---->|
        |                          |                          |    or Batch (30s+)   |
        |                          |                          |<--- JSON response ---|
        |                          |<--- result -------------|                       |
        |<--- tool result --------|                          |                       |
```

```
unityctl.slnx
+-- src/Unityctl.Shared   (netstandard2.1)  Protocol + models
+-- src/Unityctl.Core     (net10.0)         Business logic
+-- src/Unityctl.Cli      (net10.0)         CLI shell
+-- src/Unityctl.Mcp      (net10.0)         MCP server
+-- src/Unityctl.Plugin   (Unity UPM)       Editor bridge (IPC server)
+-- tests/*                                 624 xUnit tests
```

---

## Platforms

| Platform | CLI | IPC Transport | Batch | CI |
|----------|-----|---------------|-------|----|
| Windows | ✅ | Named Pipe | ✅ | ✅ |
| macOS | ✅ | Unix Domain Socket | ✅ | ✅ |
| Linux | ✅ | Unix Domain Socket | ✅ | ✅ |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Unity 2021.3+](https://unity.com/download)

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

## Documentation

- [Getting Started](docs/ref/getting-started.md) — installation, setup, and common workflows
- [AI Agent Quickstart](docs/ref/ai-quickstart.md) — MCP setup and agent integration guide
- [Architecture](docs/ref/architecture-mermaid.md) — system design and transport diagrams
- [Glossary](docs/ref/glossary.md) — key terms and concepts

## Changelog

See [GitHub Releases](https://github.com/kimjuyoung1127/unityctl/releases) for version history.

## License

MIT — see [LICENSE](LICENSE)
