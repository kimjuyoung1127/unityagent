# unityctl 프로젝트 상태

최종 업데이트: 2026-03-20 (KST)
기준 문서: `CLAUDE.md`, `docs/ref/phase-roadmap.md`, `docs/internal/DEVELOPMENT.md`

## 현재 Phase

- **Phase 0 ~ 2C**: 완료
- **Phase 3B (Flight Recorder)**: 완료
- **Phase 3A (Session Layer)**: 완료
- **Phase 4A (Ghost Mode)**: 완료
- **Phase 3C (Watch Mode)**: 완료
- **Phase 4B (Scene Diff)**: 완료
- **Phase 5 (Agent Layer)**: 완료
- **Phase 1C (CI/CD)**: 완료

- **MCP Hybrid (unityctl_run + schema filter)**: 완료
- **Write API Phase C (커버리지 확장)**: 완료
- **Script Editing v1 (create/edit/delete/validate)**: 완료
- **Diagnostics (doctor + IPC 자동 진단)**: 완료
- **Read API P0 Slice 1 (asset/gameobject/component query + includeInactive + ExploreTool)**: 완료
- **Read API P0 Slice 2 (scene hierarchy + build-settings get-scenes)**: 완료
- **Read API P0 Slice 3 (asset reference graph v1)**: 완료
- **Build Profile / Build Target Control (`build-profile *`, `build-target switch`)**: 완료
- **P3 Screenshot / Visual Feedback**: 완료
- **P2 Batch Execute / Transaction (`batch execute`)**: 완료
- **Tags & Layers + Editor Utility (tag/layer/console/define-symbols 10개 명령)**: 완료
- **Lighting & NavMesh (lighting 5개 + navmesh 3개 = 8개 명령)**: 완료
- **Physics Settings (physics 4개 명령)**: 완료
- **Editor Utility 확장 + Script List (editor pause/focus-gameview/focus-sceneview + script list 4개 명령)**: 완료

- **MCP Context Optimization (C1 QueryTool + C2 Schema Category + C3 Description 경량화)**: 완료
- **Script Patch v2 (script patch — 줄 단위 부분 편집)**: 완료
- **Script v2 (script-get-errors, script-find-refs, script-rename-symbol)**: 완료
- **UI Read Slice 1 (`ui find`, `ui get`, UGUI-first)**: 구현 완료
- **UI Interaction Slice 1 (`ui toggle`, `ui input`, deterministic state set)**: 구현 완료
- **Mesh Primitive Create (`mesh create-primitive`)**: 구현 완료
- **Multi-Instance Routing Phase 1 (`editor current/select`, `editor instances`, `editor select --pid` + project-path selection fallback + target metadata)**: 구현 완료
- **Production Domain Expansion (camera list/get, texture get/set-import-settings, scriptableobject find/get/set-property, shader find/get-properties — 9개 명령)**: 구현 완료
- **Visual Verification v2 Phase 1 (`workflow verify` — `projectValidate` + `capture` + `imageDiff` + `consoleWatch` + `uiAssert`)**: 구현 완료
- **Visual Verification v2 Phase 1.5 (`playSmoke` — play start/settle + console evidence + game-camera capture)**: 구현 완료

**전체 Phase 완료. 총 71개 write allowlist 명령, 131개 CLI 명령, 12개 MCP 도구 (33→12 통합).**

## Visual Verification v2 Phase 1 라이브 검증 (My project, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| `workflow verify --file verify.json --project "C:\\Users\\ezen601\\Desktop\\Jason\\My project" --json` | ✅ | `passed=true`, verification bundle 완료 |
| `projectValidate` step | ✅ | `All 6 checks passed` |
| `uiAssert` step | ✅ | `toggle.isOn == false` live assertion 통과, artifact 생성 |
| `capture` step 2개 | ✅ | `baseline.png`, `current.png` artifact 생성 |
| `imageDiff` step | ✅ | `changedPixelRatio=0`, diff artifact 생성 |
| `consoleWatch` step | ✅ | 1초 window에서 console artifact 수집 |
| artifact-first output | ✅ | `~/.unityctl/verification/<timestamp>-my-project-ui-verify` 하위 저장 |

검증 메모:

- 이번 slice는 `projectValidate`, `capture`, `imageDiff`, `consoleWatch`, `uiAssert`를 포함한다.
- `uiAssert`는 existing `ui get`를 evidence source로 사용하며, field path 비교(`toggle.isOn`) 방식으로 구현했다.
- real click과 end-of-frame capture는 Phase 2 범위다.

## Visual Verification v2 Phase 1.5 라이브 검증 (My project, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| `workflow verify --file play-smoke.json --project "C:\\Users\\ezen601\\Desktop\\Jason\\My project" --json` | ✅ | `passed=true`, play smoke bundle 완료 |
| `playSmoke` step | ✅ | Play 진입 settle 확인 (`settled=true`) |
| console evidence | ✅ | watch window 동안 `eventCount=1` 수집 |
| play stop | ✅ | `stopSuccess=true` |
| game capture artifact | ✅ | `smoke-game.png` 생성, `captureArtifactId=smoke-game` 반환 |

검증 메모:

- 이번 단계의 `playSmoke`는 `play start` -> `status.isPlaying` polling -> `consoleWatch` -> `play stop` -> `game-camera capture` 조합이다.
- 아직 `WaitForEndOfFrame` 기반 end-of-frame capture는 아니다.
- 따라서 현재 구현은 **play-mode smoke + game-camera capture after settle** 로 문서화한다.

## Multi-Instance Routing Phase 1 라이브 검증 (My project, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| `editor current --json` (선택 전) | ✅ | `selected=false`, 안내 메시지 반환 |
| `ping --json` (선택 전) | ✅ | `InvalidParameters[502]`, selection 안내 반환 |
| `doctor --json` (선택 전) | ✅ | `InvalidParameters[502]`, selection 안내 반환 |
| `editor select --project "C:\\Users\\ezen601\\Desktop\\Jason\\My project"` | ✅ | project-path selection 저장, pipe/editor metadata 반환 |
| `editor select --pid <running-pid>` | ⚠️ | code path 구현 완료. 이 워크스테이션에서는 AppLocker가 새 CLI assembly load를 차단해 live CLI validation은 제한됨 |
| `editor current --json` (선택 후) | ✅ | selected project + pipeName + editorVersion/location 반환 |
| `editor instances --json` | ✅ | running Unity PID / projectPath / pipeName / ipcReady inventory 반환 |
| `ping --json` (선택 후, `--project` 생략) | ✅ | IPC success, `data.target.transport=ipc` 포함 |
| `status --json` (선택 후, `--project` 생략) | ✅ | `Ready`, `data.target.projectPath/pipeName/editorVersion` 포함 |
| `check --json` (선택 후, `--project` 생략) | ✅ | `Compilation check passed`, target metadata 포함 |
| `doctor --json` (선택 후, `--project` 생략) | ✅ | `selection.matchesRequestedProject=true` 포함 |

검증 메모:

- Phase 1의 canonical selection key는 **project path**다.
- 이유:
- 현재 pipe name이 normalized project path 기반이다.
- running process detection (`pid` 기반 true instance selection)은 아직 platform 구현이 비어 있다.
- 따라서 이번 slice는 `editor current/select`, `editor instances`, `editor select --pid` + project-less CLI fallback (`ping/status/check/doctor`) + target metadata에 집중했다.
- `editor select --pid`는 **단일 project로 해석되는 running pid**에만 허용한다.
- 같은 project에 대해 여러 Unity 프로세스가 동시에 열려 있으면 true pid pinning을 보장할 수 없으므로 명시적으로 거절한다.
- 남은 Phase 2 범위:
  - running editor inventory
  - true editor-instance identity (`pid` 등)
  - session/task별 editor pin 고도화

## Mesh Primitive Create 라이브 검증 (2026-03-19)

구현 범위:

- `mesh create-primitive` — Unity built-in primitive 생성 (`Cube`, `Sphere`, `Plane`, `Cylinder`, `Capsule`, `Quad`)
- 선택 파라미터: `name`, `position`, `rotation`, `scale`, `material`, `parent`
- MCP `unityctl_run` allowlist에 `mesh-create-primitive` 추가

자동 검증:

- `dotnet build unityctl.slnx -c Release -m:1 /p:UseSharedCompilation=false` ✅
- `dotnet test tests/Unityctl.Cli.Tests -c Release --filter MeshCommandTests` ✅ 11 통과
- `dotnet test tests/Unityctl.Shared.Tests -c Release --filter "CommandCatalogTests|CommandSyncGuardrailTests"` ✅ 17 통과
- `unityctl tools --json` 기준 총 **118**개 명령, `mesh create-primitive` 노출 확인 ✅

실측 메모:

- 초기에는 `robotapp` live Editor가 stale compile state를 잡고 있어 `Unknown command [501]`와 `Busy[103]`가 교차했다.
- `MeshCreatePrimitiveHandler`의 `GlobalObjectIdResolver` import 수정 후, `doctor --json`은 `classification=healthy`, `ipc.connected=true`, Editor.log compile error 없음으로 회복됐다.
- 같은 상태에서 `mesh create-primitive --type Cube --name "CodexMeshProbe" --position "[1,2,3]" --scale "[2,1,2]" --json` 성공
- 직후 `gameobject find --name "CodexMeshProbe" --json`으로 생성된 `MeshFilter`, `MeshRenderer`, `BoxCollider` 포함 객체 확인
- unsaved scratch scene에서는 생성 결과의 `globalObjectId`가 `GlobalObjectId_V1-0-...-0-0`로 보였다. 이건 mesh 전용 문제가 아니라 저장되지 않은 scene object targeting 한계로 기록한다.

해석:

- `mesh create-primitive`는 현재 **running Editor + IPC ready** 경로에서 실측 검증 완료로 문서화할 수 있다.
- built-in primitive blockout 용도로는 충분하지만, custom mesh authoring이나 modeling workflow까지 의미하진 않는다.

## Project Validate 라이브 검증 (robotapp, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| `project validate` (정상) | ✅ | 6/6 체크 통과, valid=true |
| `project validate` (에러) | ✅ | compile=false, consoleErrors=false, valid=false |

## UI Read Slice 1 초기 검증 (2026-03-19)

구현 범위:

- `ui find` — UGUI 요소 탐색 (`name`, `text`, `type`, `parent`, `canvas`, `interactable`, `active`, `includeInactive`, `limit`)
- `ui get` — `RectTransform` + `Canvas`/`Selectable`/대표 UGUI component 상태 읽기
- MCP `unityctl_query` allowlist에 `ui-find`, `ui-get` 추가

자동 검증:

- `dotnet build src/Unityctl.Cli/Unityctl.Cli.csproj -c Release` ✅
- `dotnet test tests/Unityctl.Shared.Tests -c Release --filter "CommandCatalogTests|CommandSyncGuardrailTests|CommandSchemaTests"` ✅ 26 통과
- `dotnet test tests/Unityctl.Cli.Tests -c Release --filter "UiCommandTests|GameObjectCommandTests|ComponentCommandTests"` ✅ 72 통과
- `unityctl tools --json` 기준 `ui-find`, `ui-get` 노출 확인 ✅

실측 메모:

- `robotapp` + running Editor 상태에서 `ui find --type Canvas`는 현재 `Busy[103]` (`IPC not ready yet`)로 실패
- 같은 시점 `doctor --json`은 `classification=starting-or-reloading`, `ipc.connected=false`, Editor.log `Registered 106 commands`
- repo `SampleUnityProject`의 closed-editor batch fallback `ui find --type Canvas`는 현재 `Unity exited with code 1 but no response file was written.` 로 실패

해석:

- `ui find/get`의 첫 성공 경로는 현재도 **running Editor + IPC ready** 기준으로 안내하는 것이 맞다.
- batch fallback에서 UI read를 일반 보장으로 문서화하긴 아직 이르다.

## UI Interaction Slice 1 라이브 검증 (2026-03-19)

구현 범위:

- `ui toggle` — `Toggle.isOn`을 결정적으로 설정 (`--mode auto|edit|play`)
- `ui input` — `InputField.text`를 결정적으로 설정 (`--mode auto|edit|play`)
- MCP `unityctl_run` allowlist에 `ui-toggle`, `ui-input` 추가

자동 검증:

- `dotnet build unityctl.slnx -c Release -m:1 /p:UseSharedCompilation=false` ✅
- `dotnet test tests/Unityctl.Shared.Tests -c Release` ✅ 70 통과
- `dotnet test tests/Unityctl.Cli.Tests -c Release --no-build` ✅ 379 통과
- `dotnet test tests/Unityctl.Core.Tests -c Release --no-build` ✅ 121 통과
- `dotnet test tests/Unityctl.Mcp.Tests -c Release --no-build` ✅ 22 통과
- `unityctl tools --json` 기준 총 **117**개 명령, `ui-toggle`, `ui-input` 노출 확인 ✅

실측 메모:

- `robotapp` + IPC ready 상태에서 `doctor --json` → `classification=healthy`, `lockSeverity=informational`
- unsaved scratch scene에서 새 UI root들은 `GlobalObjectId_V1-0-...-0-0`로 보여 stable targeting이 어려웠고, 저장된 scene asset을 열어야 parent-target UI interaction 실측이 가능했다.
- 저장된 임시 scene(`Assets/CodexUiInteractionValidation_20260319223220.unity`)에서:
  - `ui toggle --mode auto` → `modeApplied=edit`, `currentValue=true`
  - `ui input --mode auto` → `modeApplied=edit`, `currentText="Alpha Beta"`
  - 이후 `ui get` readback으로 `toggle.isOn=true`, `inputField.text="Alpha Beta"` 확인
- 첫 시도에서는 `ui-toggle` / `ui-input`가 `Unknown command [501]`였고, `UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation()` 이후 registry reload 뒤 정상 응답했다.
- `robotapp`는 Play Mode 진입 시 `Assets/Scenes/Onboarding.unity`로 활성 씬이 전환되어, edit-mode 검증용 임시 UI scene이 unload되었다. 이 프로젝트에서는 full play-mode success path를 끝까지 재현하지 못했고, 대신 explicit mode guard(`--mode play`는 실제 Play Mode 필요)는 확인했다.

해석:

- `ui toggle` / `ui input`은 현재 **deterministic state set**으로 문서화하는 게 맞다.
- real click / user typing simulation은 아직 아니며, 다음 단계 `ui-click` 전용 event-dispatch helper가 필요하다.
- `file:` plugin source를 쓰는 live Editor 세션에서는 새 handler가 codebase에 있어도 domain reload 전엔 `Unknown command`가 날 수 있다.

## 라이브 검증 (최신)

> 이전 슬라이스의 라이브 검증 아카이브는 `docs/internal/DEVELOPMENT.md` "라이브 검증 아카이브" 섹션 참조.

## Script v2 라이브 검증 (robotapp, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| `script get-errors` (에러 없음) | ✅ | errorCount=0, warningCount=0, compiledAt 정상 |
| `script get-errors` (에러 있음) | ✅ | CS1525, file/line/column/code/message/assembly 전부 반환 |
| `script get-errors --path` 필터 | ✅ | 특정 파일 에러만 필터링 |
| `script find-refs --symbol MonoBehaviour` | ✅ | 10개 참조, 파일/줄/컬럼/컨텍스트 |
| `script find-refs --symbol BrokenTest` | ✅ | 1036개 파일 스캔, 단어 경계 매칭 정상 |
| `script rename-symbol --dry-run` | ✅ | 파일명 변경 미리보기 포함 |
| `script rename-symbol` (실제) | ✅ | 클래스명+파일명 변경, 리컴파일 에러 0 |

readiness 메모:

- `script get-errors`, `script find-refs`, `script rename-symbol`은 현재 **running Editor + IPC ready** 상태에서 가장 신뢰도가 높다.
- `script get-errors`는 compile cache가 아직 없으면 stale empty 결과를 낼 수 있으므로, Editor가 Ready인데도 데이터가 비어 있으면 `unityctl script validate --project <path> --wait`를 한 번 권장한다.
- `script find-refs` / `script rename-symbol`은 batch fallback보다 IPC 경로를 우선 안내하도록 CLI와 `doctor` recommendation을 보강했다.

## Script Patch v2 라이브 검증 (robotapp, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| 줄 삽입 (insert) | ✅ | startLine=5, deleteCount=0, field 추가 |
| 줄 교체 (replace) | ✅ | 1줄 삭제 + 2줄 삽입, 멀티라인 `\n` 정상 |
| 줄 삭제 (delete) | ✅ | startLine=6, deleteCount=1 |
| 범위 초과 에러 | ✅ | startLine=100 → InvalidParameters 반환 |
| patch → check 연동 | ✅ | 패치 후 컴파일 성공 확인 |

## MCP Context Optimization 라이브 검증 (robotapp, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| `asset-find` (via QueryTool) | ✅ | 50개 Scene asset 반환 |
| `gameobject-find` (via QueryTool) | ✅ | Main Camera 반환 |
| `tag-list` (via QueryTool) | ✅ | 11개 tag 반환 |
| `physics-get-settings` (via QueryTool) | ✅ | gravity, solver 등 30+ 프로퍼티 |
| `scene-hierarchy` (via QueryTool) | ✅ | 씬 트리 정상 |
| `screenshot capture` (via QueryTool) | ✅ | 1920x1080 PNG base64 |
| MCP `tools/list` 12개 도구 | ✅ | 33→12 통합 확인 |
| `unityctl_schema(category=query)` | ✅ | 카테고리 필터 동작 |
| `unityctl_query` allowlist 통과 | ✅ | asset-find 등 허용 |
| `unityctl_query` allowlist 차단 | ✅ | play-mode 등 거부 |

## Editor Utility 확장 + Script List 라이브 검증 (My project, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| `editor pause` (toggle) | ✅ | isPaused: true 반환, 토글 동작 확인 |
| `editor pause --action unpause` | ✅ | isPaused: false 반환 |
| `editor pause --action pause` | ✅ | isPaused: true 반환, 멱등성 확인 |
| `editor focus-gameview` | ✅ | focused: true, Game View 탭 활성화 확인 |
| `editor focus-sceneview` | ✅ | focused: true, Scene View 탭 활성화 확인 |
| `script list` | ✅ | 6182개 MonoScript 탐색 (Packages 포함) |
| `script list --filter Fps --limit 5` | ✅ | FpsText 1개 반환, 이름 필터 정상 |
| `script list --folder Assets --limit 5` | ✅ | Assets 폴더 내 3개 반환, 폴더 필터 정상 |

## Physics Settings 라이브 검증 (My project, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| `physics get-settings` | ✅ | DynamicsManager 전체 프로퍼티 덤프 (gravity, solver, cloth 등 30+ 프로퍼티) |
| `physics set-settings --property m_Gravity --value "[0,-20,0]"` | ✅ | Vector3 gravity 변경 성공 |
| `physics set-settings --property m_Gravity --value "[0,-9.81,0]"` | ✅ | gravity 원복 성공 |
| `physics set-settings --property m_DefaultSolverIterations --value "12"` | ✅ | int 프로퍼티 변경 성공 |
| `physics set-settings --property m_QueriesHitTriggers --value "false"` | ✅ | bool 프로퍼티 변경 성공 |
| `physics get-collision-matrix` | ✅ | 32×32 매트릭스, ignoredPairs 빈 배열 (기본 상태) |
| `physics set-collision-matrix --layer1 8 --layer2 9 --ignore true` | ✅ | TestLayer↔9 충돌 비활성화, confirmed=true |
| `physics get-collision-matrix` (변경 후) | ✅ | ignoredPairs에 layer 8↔9 표시 확인 |
| `physics set-collision-matrix --layer1 8 --layer2 9 --ignore false` | ✅ | 원복, confirmed=true |

## Tags & Layers + Editor Utility 라이브 검증 (My project, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| `tag list` | ✅ | 7개 기본 태그 확인 (Untagged, Respawn, Finish, EditorOnly, MainCamera, Player, GameController) |
| `tag add --name TestTag` | ✅ | 태그 추가 후 재조회 시 8개 확인 |
| `layer list` | ✅ | 32슬롯, 0-7 builtIn 플래그 정상 |
| `layer set --index 8 --name TestLayer` | ✅ | 유저 레이어 설정 후 재조회 확인 |
| `gameobject set-tag` | ✅ | Directional Light → TestTag, Undo 지원 |
| `gameobject set-layer` | ✅ | Directional Light → layer 8 (TestLayer), name→index 변환 정상 |
| `console get-count` | ✅ | 4 logs, 38 warnings, 0 errors 반환 |
| `console clear` | ✅ | 콘솔 클리어 후 42→1 확인 |
| `define-symbols get` | ✅ | Standalone 타겟, 빈 심볼 확인 |
| `define-symbols set --symbols "TEST_SYMBOL;DEBUG_MODE"` | ✅ | 심볼 설정 후 재조회 시 2개 확인 |
| `define-symbols set --symbols ""` | ✅ | 심볼 원복 (domain reload 트리거 확인) |
| `console get-count` (clear 후) | ✅ | 클리어 후 카운트 감소 확인 |
| `gameobject set-layer --layer TestLayer` (이름 기반) | ✅ | layer name → index 자동 변환 정상 |

## P2 Batch Execute Undo Coverage 추가 검증 (My project, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| `material set` direct undo | ✅ | `_BaseColor` 변경 후 `undo`로 `{1,1,1,1}` 복구 |
| `material set-shader` direct undo | ✅ | `Standard` 변경 후 `undo`로 `Universal Render Pipeline/Lit` 복구 |
| `project-settings set` direct undo | ✅ | `editor.m_AssetNamingUsesSpace=false` 후 `undo`로 `True` 복구 |
| `prefab unpack` direct undo | ✅ | `SM_Bld_Apartment_02` unpack 후 `undo`, 후속 `prefab apply` 성공 |
| `batch execute` rollback (`material-set` + `project-settings set`) | ✅ | forced failure 후 material color + setting 모두 원복 |
| `batch execute` rollback (`prefab-unpack`) | ✅ | forced failure 후 `prefab apply` 성공으로 instance 상태 복구 확인 |
| `batch execute` rollback (`asset-create`) | ✅ | forced failure 후 `asset get-info` not found |
| `batch execute` rollback (`asset-copy`) | ✅ | forced failure 후 `asset get-info` not found |
| `batch execute` rollback (`asset-move`) | ✅ | forced failure 후 source restored, destination not found |
| `asset create` direct undo | ⚠️ | 생성은 성공했지만 `undo` 후에도 asset file이 남음 |
| `asset set-labels` direct undo | ⚠️ | labels 변경 후 `undo` 해도 label이 남음 |

검증 메모:

- 이번 추가 검증으로 `batch execute`의 transactional safe subset을 2채널로 다시 정의했다.
- 현재 rollback 보장 범위:
  - Undo-backed: `gameobject-*`, `component-*`, selected `ui-*`, `material-set`, `material-set-shader`, `player-settings`, `project-settings set`, `prefab unpack`
  - Compensation-backed: `asset-create`, `asset-copy`, `asset-move`
- 제외한 범위:
  - `asset-set-labels`, `material-create`, `prefab-create`
- `asset create`는 별도 사용성 버그도 함께 수정했다. 프로젝트 타입(`Assembly-CSharp` 등)도 `asset create --type <TypeName>`으로 찾도록 개선했고, direct undo와 batch compensation을 분리해 검증했다.

## IPC Self-Healing 추가 검증 (My project, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| held pipe connection 1개 + `ping` | ✅ | 20초간 raw pipe hold 상태에서도 IPC 응답 유지 |
| held pipe connection 1개 + `status` | ✅ | 같은 조건에서 batch fallback 없이 `Ready` 응답 유지 |

검증 메모:

- `IpcServer`는 더 이상 accepted connection을 listener thread에서 끝까지 처리하지 않는다.
- connected pipe를 worker로 넘기고 listener는 즉시 다음 instance를 다시 listen 한다.
- 이번 수정으로 probe-first 모델에서 단일 stuck client가 전체 IPC를 막는 현상을 완화했다.

## 벤치마크 결과 (median, ms)

| 작업 | dotnet run | published exe | Unityctl.Mcp | CoplayDev MCP |
|------|-----------|---------------|--------------|---------------|
| ping | 2015 | 304 | 100 | 1 |
| editor_state | 2095 | 303 | 100 | 100 |
| active_scene | 2094 | 304 | 99 | 100 |
| diagnostic | 2053 | 401 | 101 | 100 |

Unityctl.Mcp resident mode는 `editor_state` / `active_scene` 기준 CoplayDev와 동등한 100ms대다.
단, clean-state 재실행에서는 `active_scene` 경로가 양쪽 모두 빈 문자열이었으므로 scene-path 자체를 비교 근거로 쓰지는 않는다.

## 자동화 검증

| 항목 | 상태 | 비고 |
|------|------|------|
| `dotnet build unityctl.slnx -c Release` | ✅ | 경고/오류 없이 통과 |
| `dotnet test tests/Unityctl.Shared.Tests -c Debug` | ✅ | 71 통과 |
| `dotnet test tests/Unityctl.Core.Tests -c Release` | ⚠️ | 새 slice targeted tests는 통과. full suite는 `FlightLogRobustnessTests.Query_FilterByUntil_ExcludesNewerEntries` 1건 flaky failure 확인 |
| `dotnet test tests/Unityctl.Cli.Tests -c Debug` | ✅ | 393 통과 |
| `dotnet test tests/Unityctl.Mcp.Tests -c Debug` | ✅ | 22 통과 |
| `dotnet test unityctl.slnx -c Release` | ⚠️ | Integration/환경 락, AppLocker 등 워크스테이션 조건에 따라 개별 프로젝트 실행이 더 안정적 |

| 프로젝트 | 통과 |
|----------|------|
| Unityctl.Shared.Tests | 71 |
| Unityctl.Core.Tests | 129 |
| Unityctl.Cli.Tests | 444 |
| Unityctl.Mcp.Tests | 22 |
| Unityctl.Integration.Tests | 23 (환경 의존 3개 실패 가능) |

테스트 인벤토리 기준 합계는 **689개**다.

신규 자동 검증:

- `Unityctl.Mcp.Tests`에 built `unityctl-mcp.exe` 기준 `initialize` / `tools/list` / `unityctl_schema` / `unityctl_run` allowlist/parameters / invalid tool / missing arg black-box 테스트 추가 (16개)
- `Unityctl.Integration.Tests`에 repo-contained `SampleUnityProject` 기반 closed-editor `status` / `check` / `test --mode edit` / `build --dry-run` 검증 추가
- `Unityctl.Shared.Tests`에 `ExecHandler` 실제 grammar/security/parse contract 테스트 추가
- `.github/workflows/ci-dotnet.yml`에 published CLI smoke (`--help`, `schema`, `tools --json`) 추가

> 이전 실측 상세/경쟁 분석 아카이브 → `docs/internal/DEVELOPMENT.md` "라이브 검증 아카이브" 섹션 참조.

## NuGet v0.2.0 배포 완료 (2026-03-19)

| 항목 | 결과 |
|------|------|
| `dotnet pack` (CLI) | ✅ `unityctl.0.2.0.nupkg` (666KB) |
| `dotnet pack` (MCP) | ✅ `unityctl-mcp.0.2.0.nupkg` (1.6MB) |
| `dotnet tool install -g unityctl` | ✅ NuGet.org 배포 완료 |
| `dotnet tool install -g unityctl-mcp` | ✅ NuGet.org 배포 완료 |
| GitHub Actions release.yml | ✅ 4플랫폼 빌드 + NuGet push + GitHub Release 자동화 |
| GitHub Release v0.2.0 | ✅ Win/Mac(x64+arm64)/Linux 바이너리 첨부 |

설치: `dotnet tool install -g unityctl && dotnet tool install -g unityctl-mcp`

배포 현실 메모:

- GitHub Release CLI 아카이브는 현재 framework-dependent publish 결과물이다 (`release.yml`의 `--self-contained false`).
- `unityctl init`의 기본 동작은 여전히 로컬 `Unityctl.Plugin` workspace 탐색이지만, 명시적 `--source <git-url>` additive 경로를 지원한다.

## 설치/온보딩 현실 검증 (2026-03-19)

판정: **현재 그대로 가능 아님. additive rollout 진행 중.**

근거:

- fresh artifact 기준을 고정하기 위해 `dotnet build unityctl.slnx -c Release`와 `dotnet publish ... -o artifacts/investigation/*`를 다시 실행했다.
- 이전 로컬 `src/Unityctl.Cli/bin/Release/net10.0/unityctl.exe`는 stale binary였고, 최신 소스의 `doctor`, `asset find`, `scene hierarchy` 등을 포함하지 않았다.
- `InitCommand` + `PluginSourceLocator` 기준 현재 `init`는 기본적으로 로컬 `src/Unityctl.Plugin`를 찾는 bootstrap이며, 명시적 Git URL source는 additive 옵션으로 지원한다.

실측:

| 환경 | 명령 | 결과 |
|------|------|------|
| `robotapp`, Unity Editor 실행 + IPC 준비 | `ping --json` 3회 | 856 / 999 / 1108ms, median **999ms**, `pong` |
| `robotapp`, Unity Editor 실행 + IPC 준비 | `status --json` 3회 | 844 / 939 / 1139ms, median **939ms**, `Ready` |
| `robotapp`, same state | `doctor --json` | `plugin.installed=true`, `ipc.connected=true` |
| `robotapp`, same state | `init` 재실행 | `com.unityctl.bridge is already in manifest.json` |
| repo `SampleUnityProject`, IPC 미준비 / batch fallback | `ping --json` 3회 | 10866 / 11023 / 17618ms, median **11023ms**, 실패 |
| repo `SampleUnityProject`, IPC 미준비 / batch fallback | `status --json` 3회 | 12540 / 19700 / 20004ms, median **19700ms**, 실패 |
| repo `SampleUnityProject`, IPC 미준비 / batch fallback | `check --json` | 실패 (`Unity exited with code 1 but no response file was written.`) |

결론:

- 현재 문서에 `플랫폼별 단일 바이너리 설치`, `zero-dependency install`, `1분 안 첫 성공`을 일반 약속으로 쓰기엔 근거가 부족하다.
- 현재 구현에서 안전하게 말할 수 있는 것은:
  - CLI/MCP는 `dotnet tool install`로 설치 가능하다.
  - `init`는 로컬 plugin source 또는 명시적 Git URL source로 bootstrap 된다.
  - `ping`/`status` 첫 성공은 **running Editor + IPC ready** 상태에서 가장 현실적이다.
  - headless/batch fallback은 유의미하지만 프로젝트별 실패 가능성을 남긴다.

추가 보강:

- `doctor`가 plugin source kind (`local-file` / `git`), recent failure summary, active session hint, 추천 액션을 함께 보고하도록 확장했다.
- `doctor`는 read-only를 유지하며, IPC가 이미 연결된 상태에서는 Unity lockfile을 informational로 취급한다.
- `Unityctl.Shared.Tests`에 command sync guardrail을 추가해 Shared `WellKnownCommands`, Plugin 공유 복사본, Plugin handler coverage, script 계열 CLI/MCP registration drift를 정적 테스트로 잡도록 보강했다.
- `script` diagnostics/refactor 명령은 locked + IPC 미준비 상태에서 script 전용 readiness 안내를 먼저 반환하고, `doctor`는 `script get-errors`에 대해 `script validate --wait` follow-up을 별도로 추천하도록 보강했다.

## 후속 과제

1. 멀티 인스턴스 라우팅 Phase 2 (running process inventory + true editor-instance pinning)
2. workflow 번들 계층과 evidence 묶음 설계
3. Visual Verification v2 Phase 2 (end-of-frame capture/diff, click helper, richer play-mode evidence)
4. 제작 도메인 확장 우선순위 확정 (`packages`, `camera/Cinemachine`, `URP/HDRP volume`)
5. macOS / Linux 실제 테스트와 plugin 설치 경로 단순화
6. write API property alias 개선 (`mass` → `m_Mass` 등)
7. 세부 우선순위는 `docs/ref/phase-roadmap.md` "다음 개발 로드맵" 섹션과 `docs/internal/competitive-analysis-2026-03-19.md` 참조

## Transport Latency 실측 (published exe, IPC, 2026-03-18)

| 명령 | Median | Min | Max |
|------|--------|-----|-----|
| `ping` | 528ms | 481ms | 592ms |
| `status` | 523ms | 491ms | 593ms |
| `check` | 668ms | 605ms | 726ms |
| `exec` | 567ms | 499ms | 662ms |

- 첫 실행 JIT 오버헤드: ~1779ms (ping 기준)
- MCP resident mode: ~100ms (프로세스 시작 + probe 없음)
- CLI published exe 오버헤드: ~400ms (프로세스 시작 ~200ms + IPC probe ~100ms + 기타 ~100ms)
