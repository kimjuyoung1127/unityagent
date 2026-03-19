# docs-status-integrity - command/docs/status drift check

## Meta
- Task: unityctl code-to-doc integrity scan
- Schedule: 평일 10:00 (Asia/Seoul)
- Role: Detect drift between implemented command surfaces and docs/status files, then report it
- Project root: `C:\Users\ezen601\Desktop\Jason\unityctl`

## Source of Truth
- Rule chain:
  - `AGENTS.md`
  - `CLAUDE.md`
  - `docs/internal/CLAUDE.md`
  - `docs/internal/status-CLAUDE.md`
- Code truth:
  - `src/Unityctl.Shared/Commands/CommandCatalog.cs`
  - `src/Unityctl.Shared/Protocol/WellKnownCommands.cs`
  - `src/Unityctl.Cli/Program.cs`
  - `src/Unityctl.Mcp/Tools/**`
- Status and reference docs:
  - `docs/status/PROJECT-STATUS.md`
  - `docs/status/PHASE-EXECUTION-BOARD.md`
  - `docs/ref/phase-roadmap.md`
  - `docs/ref/feature-summary-post.md`
  - `docs/ref/code-patterns.md`
- Evidence docs:
  - `docs/internal/daily/**`
  - `docs/internal/weekly/**`

## Lock
- Lock file: `docs/status/.docs-status-integrity.lock`
- On start write `{"status":"running","started_at":"<ISO>"}`
- On finish write `{"status":"released","released_at":"<ISO>"}`
- If lock is already `running`, exit immediately

## Procedure

### Step 0 - Pre-check
1. Acquire the lock.
2. Confirm `DRY_RUN=true` unless explicitly promoted.
3. Read the rule chain before any comparison.

### Step 1 - Collect code facts
1. Parse `CommandCatalog.All` into `catalog_commands`.
2. Count CLI commands from `src/Unityctl.Cli/Program.cs`.
3. Enumerate MCP tool names from `src/Unityctl.Mcp/Tools/**`.
4. Record write allowlist facts from `src/Unityctl.Mcp/Tools/RunTool.cs`.

### Step 2 - Collect doc facts
1. Parse command counts and MCP counts from:
   - `docs/status/PROJECT-STATUS.md`
   - `docs/ref/feature-summary-post.md`
   - `docs/ref/phase-roadmap.md`
2. Parse named completed milestones from:
   - `docs/status/PROJECT-STATUS.md`
   - `docs/ref/phase-roadmap.md`
3. Parse any documented diagnostic behavior from `docs/ref/code-patterns.md`.

### Step 3 - Compare
1. `COMMAND_COUNT_DRIFT = code command count vs docs command count`
2. `MCP_COUNT_DRIFT = code MCP tool count vs docs MCP count`
3. `MILESTONE_DRIFT = completed code milestones vs docs completed milestones`
4. `DOC_BEHAVIOR_DRIFT = documented doctor/diagnostic behavior vs actual code`
5. `UNTRACKED_FEATURE = command/tool families present in code but absent from status/ref docs`

### Step 4 - Evidence scan
1. Read the latest folder under `docs/internal/daily/`.
2. Compare recent daily evidence to `PROJECT-STATUS.md`.
3. Record `STATUS_MISMATCH` only when evidence conflicts with current status docs.

### Step 5 - Report
1. If `DRY_RUN=true`, print report body only.
2. Otherwise write `docs/status/DOCS-STATUS-INTEGRITY-REPORT.md` with:
   - summary counts
   - drift item lists
   - source-of-truth files used
3. Append `docs/status/DOCS-STATUS-INTEGRITY-HISTORY.ndjson` with timestamped counts.

### Step 6 - Release
1. Release the lock file.

## Must Not
- Do not edit `src/` or `tests/`.
- Do not auto-change `PROJECT-STATUS.md` or `phase-roadmap.md`.
- Only report drift unless the prompt is explicitly promoted to a write-capable workflow.

## DRY_RUN=true
- Print report content only.
- Final line: `[DRY_RUN] no files changed`
