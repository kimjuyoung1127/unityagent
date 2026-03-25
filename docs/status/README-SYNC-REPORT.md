[readme-sync 완료] 2026-03-20 01:53

## 수집 사실 (Code Ground Truth)

| 항목 | 실제값 | 출처 |
|------|--------|------|
| actual_command_count | **131** | `src/Unityctl.Shared/Commands/CommandCatalog.cs` — `All[]` 배열 131개 항목 |
| actual_mcp_tool_count | **12** | `src/Unityctl.Mcp/Tools/` — `*Tool.cs` 파일 12개 |
| actual_test_count | **633** | `docs/status/PROJECT-STATUS.md` — "테스트 인벤토리 기준 합계는 **633개**" |

---

## 숫자 Drift

| 위치 | 현재값 | 정확한값 | 심각도 | 처리 |
|------|--------|----------|--------|------|
| README.md L460 아키텍처 블록 `+-- tests/*` | `624 xUnit tests` | `633 xUnit tests` | **CRITICAL** | ✅ 자동 수정 완료 |

수정 전: `+-- tests/*                                 624 xUnit tests`
수정 후: `+-- tests/*                                 633 xUnit tests`

### 정상 확인된 숫자 인스턴스

| 위치 | 내용 | 상태 |
|------|------|------|
| L10 | "131 commands" | ✅ 정확 |
| L13 | "131 CLI commands · 12 MCP tools · 633 tests" | ✅ 정확 |
| L129 | "**131** (read + write + validate + diagnose)" | ✅ 정확 |
| L143 | "The 12 MCP tools cover the full 131-command surface" | ✅ 정확 |
| L243 | "12 MCP Tools" (summary header) | ✅ 정확 |
| L264 | "## Commands (131)" | ✅ 정확 |
| L442 | "12 MCP tools   131 commands" (Architecture diagram) | ✅ 정확 |

---

## 예시 커맨드 Drift

README bash 블록 내 `unityctl <subcommand>` 전체 검토 결과:

| 예시 커맨드 | CommandCatalog 존재 | 상태 |
|-------------|---------------------|------|
| `scene create` | SceneCreate | ✅ |
| `mesh create-primitive` | MeshCreatePrimitiveCmd | ✅ |
| `gameobject create` | GameObjectCreate | ✅ |
| `component add` | ComponentAdd | ✅ |
| `scene hierarchy` | SceneHierarchy | ✅ |
| `screenshot capture` | ScreenshotCapture | ✅ |
| `project validate` | ProjectValidateCmd | ✅ |
| `gameobject set-tag` | GameObjectSetTag | ✅ |
| `script create` | ScriptCreateCmd | ✅ |
| `script patch` | ScriptPatchCmd | ✅ |
| `script validate` | ScriptValidateCmd | ✅ |
| `script get-errors` | ScriptGetErrorsCmd | ✅ |
| `batch execute` | BatchExecute | ✅ |
| `scene save` | SceneSave | ✅ |
| `build` | Build | ✅ |
| `editor select` | EditorSelect | ✅ |
| `editor current` | EditorCurrent | ✅ |
| `editor instances` | EditorInstances | ✅ |
| `ping` | Ping | ✅ |
| `status` | Status | ✅ |
| `check` | Check | ✅ |
| `doctor` | Doctor | ✅ |
| `workflow verify` | WorkflowVerify | ✅ |
| `init` | Init | ✅ |

**결과: 모든 예시 커맨드가 CommandCatalog에 존재함. EXAMPLE_COMMAND_MISSING 없음.**

---

## 그룹 합계 Drift

| 그룹 | README 표기 수 | 실제 매핑 커맨드 수 |
|------|--------------|-------------------|
| Core | 13 | 13 |
| Scene & GameObject | 19 | 19 |
| Assets & Materials | 21 | 21 |
| Scripting & Code Analysis | 10 | 10 |
| Editor Control | 18 | 18 |
| Build & Deployment | 6 | 6 |
| Physics, Lighting & NavMesh | 12 | 12 |
| UI & Mesh | 8 | 8 |
| Automation & Monitoring | 15 | 15 |
| **합계** | **122** | **122** |

**GROUP_SUM_DRIFT: true** — 그룹 합계 122 ≠ actual_command_count 131 (차이 = 9)

### 미배정 커맨드 (9개) — Production Domain Expansion 슬라이스

다음 9개 커맨드가 CommandCatalog.All에 존재하지만 Commands 섹션 어느 그룹에도 포함되지 않음:

| 커맨드 | CatalogName | 제안 그룹 |
|--------|-------------|-----------|
| `camera list` | CameraListCmd | Assets & Materials 또는 신규 그룹 |
| `camera get` | CameraGetCmd | Assets & Materials 또는 신규 그룹 |
| `texture get-import-settings` | TextureGetImportSettingsCmd | Assets & Materials |
| `texture set-import-settings` | TextureSetImportSettingsCmd | Assets & Materials |
| `scriptableobject find` | ScriptableObjectFindCmd | Assets & Materials |
| `scriptableobject get` | ScriptableObjectGetCmd | Assets & Materials |
| `scriptableobject set-property` | ScriptableObjectSetPropertyCmd | Assets & Materials |
| `shader find` | ShaderFindCmd | Assets & Materials |
| `shader get-properties` | ShaderGetPropertiesCmd | Assets & Materials |

**수동 검토 필요** — 그룹 신설(`Production Domain`) 또는 기존 그룹(Assets & Materials) 확장을 결정한 후 그룹 수와 `## Commands (131)` 일치 여부 재확인 권장.

---

## MCP 도구 Drift

README `12 MCP Tools` 테이블 (12개) vs `src/Unityctl.Mcp/Tools/` *Tool.cs 파일 (12개):

| README 도구명 | 대응 파일 | 상태 |
|---------------|-----------|------|
| `unityctl_query` | QueryTool.cs | ✅ |
| `unityctl_run` | RunTool.cs | ✅ |
| `unityctl_schema` | SchemaTool.cs | ✅ |
| `unityctl_build` | BuildTool.cs | ✅ |
| `unityctl_check` | CheckTool.cs | ✅ |
| `unityctl_test` | TestTool.cs | ✅ |
| `unityctl_exec` | ExecTool.cs | ✅ |
| `unityctl_status` | StatusTool.cs | ✅ |
| `unityctl_ping` | PingTool.cs | ✅ |
| `unityctl_watch` | WatchTool.cs | ✅ |
| `unityctl_log` | LogTool.cs | ✅ |
| `unityctl_session_list` | SessionTool.cs | ✅ |

**MCP_TOOL_MISSING 없음. MCP_TOOL_GHOST 없음.**

---

## 신규 기능 미반영

CLAUDE.md 최신 완료 슬라이스 3개와 README 반영 상태:

| 슬라이스 | 완료일 | README 반영 |
|----------|--------|-------------|
| Production Domain Expansion (camera/texture/scriptableobject/shader 9개 명령) | 2026-03-20 | ⚠️ 미반영 — Commands 섹션 어느 그룹에도 없음 |
| Multi-Instance Routing Phase 1 (editor current/select/instances) | 2026-03-20 | ✅ Core 그룹에 포함됨 |
| Mesh Create Primitive (mesh create-primitive) | 2026-03-19 | ✅ UI & Mesh 그룹에 포함됨 |

---

## 요약

- command_count_drift: **false** (실제: 131, README 헤더/상단: 131)
- mcp_tool_count_drift: **false** (실제: 12, README: 12)
- test_count_drift: **true** (실제: 633, README L460: 624 → **자동 수정 완료**)
- group_sum_drift: **true** (그룹 합계: 122, actual: 131, 차이: 9 커맨드 미배정)
- example_command_missing: **false**
- mcp_tool_missing: **false**
- mcp_tool_ghost: **false**
- feature_not_in_readme: **true** (Production Domain Expansion 9개 커맨드 미반영)
- total_issues: **3**
- auto_fixed: **1** (README L460 624 → 633)
- manual_review_needed: **2** (GROUP_SUM_DRIFT, FEATURE_NOT_IN_README — Production Domain 9개 커맨드 그룹 배정 필요)
