# AI Agent Quickstart

See also: [glossary](./glossary.md)

This guide is for AI coding agents (Claude, Copilot, etc.) that need to automate Unity projects.

## Zero-config Setup

### Option A: NuGet install (recommended)

```bash
# 1. Install
dotnet tool install -g unityctl
dotnet tool install -g unityctl-mcp   # MCP server (optional)

# 2. Add plugin to target Unity project
unityctl init --project "/path/to/unity/project"

# 3. Verify
unityctl editor list --json
```

### Option B: Build from source

```bash
# 1. Clone and build
git clone https://github.com/kimjuyoung1127/unityagent.git
cd unityagent
dotnet build unityctl.slnx

# 2. Add plugin to target Unity project
dotnet run --project src/Unityctl.Cli -- init --project "/path/to/unity/project"

# 3. Verify
dotnet run --project src/Unityctl.Cli -- editor list --json
```

## Tool Discovery

Before calling commands, discover what's available:

```bash
# Human-readable list
dotnet run --project src/Unityctl.Cli -- tools

# Machine-readable JSON (MCP-inspired discovery subset)
dotnet run --project src/Unityctl.Cli -- tools --json
```

The `--json` output returns a JSON array of tool definitions with name, description, category, and parameter schemas. This is the recommended way for AI agents to dynamically discover available commands.

Note: this is a reduced discovery format inspired by MCP `tools/list`, not the full MCP tool schema.

## Common Workflows

### Check if project compiles

```bash
dotnet run --project src/Unityctl.Cli -- check --project "/path/to/project" --json
# Exit code 0 = success, 1 = failure
```

### Verify a running Editor is reachable

```bash
dotnet run --project src/Unityctl.Cli -- ping --project "/path/to/project" --json
dotnet run --project src/Unityctl.Cli -- status --project "/path/to/project" --json
```

### Run EditMode tests

```bash
dotnet run --project src/Unityctl.Cli -- test --project "/path/to/project" --mode edit --json
```

### Build for Windows

```bash
dotnet run --project src/Unityctl.Cli -- build --project "/path/to/project" --target StandaloneWindows64 --json
```

Note: `build` still uses `--target`. It does not implicitly build with the active build profile.

### Inspect or switch build profiles / targets

```bash
dotnet run --project src/Unityctl.Cli -- build-profile list --project "/path/to/project" --json
dotnet run --project src/Unityctl.Cli -- build-profile get-active --project "/path/to/project" --json
dotnet run --project src/Unityctl.Cli -- build-target switch --project "/path/to/project" --target Android --timeout 60 --json
dotnet run --project src/Unityctl.Cli -- build-profile set-active --project "/path/to/project" --profile "platform:StandaloneWindows64" --timeout 60 --json
```

`build-profile set-active` and `build-target switch` require a running Editor with IPC connectivity.

Both commands persist transition state under `Library/Unityctl/build-state`, which lets polling survive temporary IPC disconnects during domain reload or target switch.

### Batch edit with rollback

```bash
dotnet run --project src/Unityctl.Cli -- batch execute --project "/path/to/project" --file "./batch.json" --json
```

`batch execute` sends multiple edit commands in one IPC round-trip and rolls back completed steps when a later step fails.

Current v1 constraints:

- IPC-only. It requires a running Editor and does not support closed-editor batch fallback.
- Rollback is guaranteed for the current transactional safe subset:
  - Undo-backed: `gameobject-*`, `component-*`, selected `ui-*`, `material-set`, `material-set-shader`, `player-settings`, `project-settings set`, `prefab unpack`
  - Compensation-backed: `asset-create`, `asset-copy`, `asset-move`
- Direct `asset create/copy/move` are still non-transactional. Only `batch execute` failure rollback guarantees those asset-file operations.
- Prefer `--file` for CLI usage. MCP agents can call the same protocol command through `unityctl_run(command="batch-execute", parameters={...})`.

## Parameters

Commands accept typed parameters via JSON payload. The CLI maps command-line flags to `JsonObject` parameters internally. Key parameter types:
- **String**: `--target StandaloneWindows64` → `request.GetParam("target")`
- **Bool**: handler-side `request.GetParam<bool>("verbose")`
- **Int**: handler-side `request.GetParam<int>("count")`
- **Nested**: handler-side `request.GetObjectParam("options")`

## StatusCode Reference

| Code | Name | Meaning | Action |
|------|------|---------|--------|
| 0 | Ready | Success | Done |
| 100-103 | Transient | Unity is busy | Retry (auto with --wait) |
| 200 | NotFound | No Unity installed | Install Unity |
| 201 | ProjectLocked | Batch fallback cannot take the project lock | Use IPC with `ping`/`status` or close the Editor |
| 203 | PluginNotInstalled | Plugin missing | Run `init` |
| 500+ | Error | Something broke | Check logs |

## Transport Selection

The `CommandExecutor` selects transport automatically:
1. **IPC** (Phase 2B): if Unity Editor is running with plugin → low-latency response (best-effort, not hard-guaranteed)
2. **Batch**: spawn Unity in batchmode → 30-120s response

Current state:

- IPC transport is implemented and used for `ping`, `status`, `check`, and `test-start` when a matching Editor is running
- Batch remains the fallback path
- `build` is also routed through the running Editor path; success still depends on the Unity project's own compile state

## MCP Compatibility

unityctl is designed as a **superset** of MCP for Unity workflows:

| MCP Feature | unityctl Equivalent | Status |
|-------------|-------------------|--------|
| `tools/list` | `unityctl tools --json` | Available |
| Tool execution | CLI commands + `--json` | Available |
| Prompts | `ai-quickstart.md` | Available |
| Resources | Flight Recorder + Scene Snapshot | ✅ Available |
| Tasks | Session Layer | ✅ Available |
| Streaming | Watch Mode | ✅ Available |

The native MCP server (`unityctl-mcp`) is available as a dotnet tool with 33 MCP tools.

## Error Recovery

If a command fails, check:
1. `editor list` — is Unity installed?
2. `init --project <path>` — is the plugin installed?
3. `ping --project <path>` — is the running Editor reachable over IPC?
4. `doctor --project <path> --json` — inspect `ipc`, `editorLog`, and `buildState` before retrying
5. Check the log file path in the error output for Unity logs.
