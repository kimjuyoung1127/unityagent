# readme-sync - README 정합성 검증 및 동기화

## Meta
- Task: unityctl README drift scan
- Schedule: 평일 10:45 (Asia/Seoul)
- Role: README.md의 숫자/예시/섹션이 코드 실제 상태와 일치하는지 감지하고 보고
- Project root: `C:\Users\ezen601\Desktop\Jason\unityctl`

## Source of Truth
- 코드 진실:
  - `src/Unityctl.Shared/Commands/CommandCatalog.cs` — 전체 커맨드 목록
  - `src/Unityctl.Cli/Program.cs` — CLI 등록 커맨드
  - `src/Unityctl.Mcp/Tools/**` — MCP 도구 파일 목록
  - `src/Unityctl.Mcp/Tools/RunTool.cs` — write allowlist
- 문서 진실:
  - `CLAUDE.md` — 커맨드 수, 테스트 수, Phase 최신 상태
  - `docs/status/PROJECT-STATUS.md` — 커맨드/테스트 수 기준값
- 검증 대상:
  - `README.md`

## Lock
- Lock file: `docs/status/.readme-sync.lock`
- On start: write `{"status":"running","started_at":"<ISO>"}`
- On finish: write `{"status":"released","released_at":"<ISO>"}`
- lock이 `running` 상태이고 2시간 이내면 즉시 종료

---

## Procedure

### Step 0 — Pre-check
1. Lock 획득.
2. `DRY_RUN=true` 기본값 확인.
3. `CommandCatalog.cs`, `Program.cs`, `src/Unityctl.Mcp/Tools/` 읽기.

---

### Step 1 — 코드 사실 수집

| 항목 | 수집 방법 |
|---|---|
| `actual_command_count` | `CommandCatalog.All` 항목 수 |
| `actual_mcp_tool_count` | `src/Unityctl.Mcp/Tools/` 내 `*Tool.cs` 파일 수 |
| `actual_test_count` | `docs/status/PROJECT-STATUS.md` 또는 `CLAUDE.md` 최신 기재값 (dotnet test 실행은 생략 가능) |
| `cli_command_groups` | `Program.cs` 에서 최상위 커맨드 그룹 목록 |
| `write_allowlist` | `RunTool.cs` allowlist 항목 수 |

---

### Step 2 — README 사실 수집

README.md에서 아래 항목을 추출한다.

#### 2-1. 숫자 인스턴스 (Number Instances)
README 전체에서 아래 패턴이 등장하는 **모든 줄 번호와 원문**을 기록한다:

| 패턴 | 예시 |
|---|---|
| `N commands` | `118 commands`, `**118** (read + write…)`, `## Commands (118)` |
| `N MCP tools` | `12 MCP tools`, `12 MCP tools cover…` |
| `N tests` | `624 tests`, `624 xUnit tests` |
| `Core (N)` | `### Core (9)` |
| command group `(N)` | `Scene & GameObject (19)` 등 각 섹션 괄호 수 |

이 목록을 `readme_number_instances` 로 저장.

#### 2-2. CLI 예시 코드블록 커맨드
README의 \`\`\`bash 코드블록에서 `unityctl <subcommand>` 패턴을 모두 추출한다.
→ `readme_example_commands` 목록으로 저장.

#### 2-3. Commands 섹션 그룹 목록
`## Commands (N)` 하위의 `### GroupName (N)` 섹션 이름 목록을 수집한다.
→ `readme_command_groups` 로 저장.

#### 2-4. MCP 도구 테이블
`<summary><strong>12 MCP Tools</strong>` 블록 내 `| \`tool_name\`` 행을 파싱한다.
→ `readme_mcp_tools` 목록으로 저장.

---

### Step 3 — 비교 및 Drift 감지

#### 3-1. 숫자 drift (CRITICAL)
```
COMMAND_COUNT_DRIFT = actual_command_count ≠ 모든 readme_number_instances의 커맨드 수
MCP_TOOL_COUNT_DRIFT = actual_mcp_tool_count ≠ 모든 readme_number_instances의 MCP 수
TEST_COUNT_DRIFT = actual_test_count ≠ 모든 readme_number_instances의 테스트 수
```
- 불일치 시: 줄 번호, 현재값, 정확한 값을 모두 기록

#### 3-2. 커맨드 예시 drift (HIGH)
- `readme_example_commands` 각 항목이 `CommandCatalog.All` 또는 `cli_command_groups`에 존재하는지 확인
- 존재하지 않는 커맨드 → `EXAMPLE_COMMAND_MISSING`

#### 3-3. Commands 섹션 그룹 drift (MEDIUM)
- `readme_command_groups`에서 각 그룹 괄호 수 합계 == `actual_command_count` 인지 확인
- 그룹 합계 불일치 → `GROUP_SUM_DRIFT`
- CLI에 있지만 README 섹션에 없는 그룹 → `UNTRACKED_GROUP`

#### 3-4. MCP 도구 테이블 drift (MEDIUM)
- `readme_mcp_tools` 수 vs `actual_mcp_tool_count` 비교
- README 테이블에 없는 실제 도구 → `MCP_TOOL_MISSING`
- README 테이블에 있지만 코드에 없는 도구 → `MCP_TOOL_GHOST`

#### 3-5. 신규 Phase/기능 미반영 (LOW)
- `CLAUDE.md` "현재 상태" 섹션의 최신 완료 Phase 3개가 README에 언급되어 있는지 확인
- 누락 → `FEATURE_NOT_IN_README`

---

### Step 4 — 수정 제안 생성 (DRY_RUN=false 시 실제 반영)

drift가 감지된 각 항목에 대해:

1. **숫자 drift** → README 내 해당 줄 번호의 `OLD_NUMBER` → `NEW_NUMBER` 치환 목록 생성
   - `DRY_RUN=false` 시 실제 Edit 수행
2. **예시 커맨드 drift** → 수동 검토 필요 태그만 붙임 (자동 수정 금지)
3. **그룹 합계 drift** → 수동 검토 필요 태그
4. **MCP 테이블 drift** → 수동 검토 필요 태그
5. **신규 기능 미반영** → README "What AI Agents Can Build" 섹션 하단에 추가 후보 목록 출력

---

### Step 5 — 보고서 작성

`DRY_RUN=true`:
- 콘솔에 아래 형식으로 출력만 수행

`DRY_RUN=false`:
- `docs/status/README-SYNC-REPORT.md` 에 보고서 작성
- `docs/status/README-SYNC-HISTORY.ndjson` 에 타임스탬프 기록 append

**출력 형식:**
```
[readme-sync 완료] YYYY-MM-DD HH:mm

## 숫자 Drift
| 위치 | 현재값 | 정확한값 | 심각도 |
|---|---|---|---|
| README:13 "118 CLI commands" | 118 | 122 | CRITICAL |

## 예시 커맨드 Drift
- [EXAMPLE_COMMAND_MISSING] line 55: `unityctl mesh create` — CommandCatalog에 없음

## 그룹 합계 Drift
- [GROUP_SUM_DRIFT] 섹션 합계 115 ≠ 실제 122

## MCP 도구 Drift
- [MCP_TOOL_MISSING] `unityctl_new_tool` — README MCP 테이블에 없음

## 신규 기능 미반영
- [FEATURE_NOT_IN_README] "Mesh Create Primitive" — CLAUDE.md에 완료 기재, README 미반영

## 요약
- command_count_drift: <true|false> (실제: N, README: N)
- mcp_tool_count_drift: <true|false>
- test_count_drift: <true|false>
- total_issues: N
- auto_fixable: N (숫자 치환만)
- manual_review_needed: N
- errors: <none|summary>
```

---

### Step 6 — Lock 해제
Lock 파일을 `{"status":"released","released_at":"<ISO>"}` 로 갱신.

---

## Must Not
- `src/` 또는 `tests/` 파일 수정 금지
- README의 **문장/설명 내용**을 자동 수정 금지 (숫자만 자동 수정 허용)
- 코드 예시 블록 자동 수정 금지 (수동 검토 필요 태그만)
- lock 파일 자동 해제 금지 (2시간 이상 stale 시에도 STALE_LOCK 보고만)

## DRY_RUN=true (기본값)
- 파일 변경 없이 drift 목록 및 수정 제안만 출력
- 마지막 줄: `[DRY_RUN] no files changed`
