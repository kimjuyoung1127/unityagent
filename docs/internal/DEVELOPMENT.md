# unityctl 개발 진행 상황

> 최종 업데이트: 2026-03-19

## 문서 구조

현재 `docs/`는 아래 구조를 기준으로 운영합니다.

- `docs/ref/` : 장기 참조 문서, 설계, 로드맵, 용어집
- `docs/status/` : 현재 phase 상태, 실행 보드, 프로젝트 상태
- `docs/daily/` : 일별 작업 기록
- `docs/weekly/` : 주간 요약
- `docs/DEVELOPMENT.md` : 현재 코드베이스 기준 단일 개발 현황 요약

새 문서를 추가하거나 기존 문서를 이동할 때는 루트가 아니라 위 분류에 맞춰 배치합니다.

---

## 프로젝트 개요

| 항목 | 값 |
|------|-----|
| 레포 위치 | `C:\Users\gmdqn\unityagent` |
| 런타임 | .NET 10 (SDK 10.0.201) |
| CLI 프레임워크 | ConsoleAppFramework 5.3.3 |
| Shared 타겟 | .NET Standard 2.1 |
| Core / CLI 타겟 | .NET 10 |
| Unity 플러그인 | UPM, Unity 2021.3+, Newtonsoft.Json 3.2.1 |
| 테스트 프레임워크 | xUnit 2.9.2 |
| 수동 검증 프로젝트 | `C:\Users\gmdqn\robotapp` (Unity 6000.0.64f1) |

주요 참조 문서:

- `docs/ref/getting-started.md`
- `docs/ref/ai-quickstart.md`
- `docs/ref/glossary.md`
- `docs/ref/phase-roadmap.md`
- `docs/ref/phase-2b-plan.md`
- `docs/status/PROJECT-STATUS.md`

---

## 아키텍처

```text
unityctl.slnx
├── src/Unityctl.Shared    (netstandard2.1)  프로토콜 + 모델 + 상수
├── src/Unityctl.Core      (net10.0)         비즈니스 로직 (transport, discovery, retry)
├── src/Unityctl.Cli       (net10.0)         얇은 CLI 셸
├── src/Unityctl.Plugin    (Unity UPM)       Editor 브릿지
├── tests/Unityctl.*.Tests xUnit 테스트
└── docs/                  ref/status/daily/weekly 구조
```

의존성 방향:

```text
Shared <- Core <- Cli
```

Plugin은 Unity 내부에서만 컴파일되며, `src/Unityctl.Plugin/Editor/Shared/`에 프로토콜 복사본을 유지합니다.

---

## Phase 현황

| Phase | 상태 | 요약 |
|-------|------|------|
| 0 ~ 1B | ✅ 완료 | 골격, Plugin, CLI 기본, 핵심 핸들러 |
| 1C | ⚠️ 부분 완료 | CI 있음, release/README 미완 |
| 2A / 2A+ | ✅ 완료 | Payload 정리, Core 추출, tools metadata |
| 2B | ✅ 완료 | IPC transport 구현, robotapp 수동 검증 완료 |
| 2C | ✅ 완료 | Async Commands — polling, single-flight, ACCEPTED [104] |
| 3B | ✅ 완료 | Flight Recorder (NDJSON 로깅, append-only, 예외 안전) |
| 3A | ✅ 완료 | Session Layer (상태머신 6개, NDJSON 저장소, MCP Tasks 매핑) |
| 4A | ✅ 완료 | Ghost Mode (--dry-run preflight, 3단계 검증) |
| 3C | ✅ 완료 | Watch Mode (Push 스트리밍, ConcurrentQueue, 영구 파이프) |
| 4B | ✅ 완료 | Scene Diff (SerializedObject, GlobalObjectId, propertyPath diff) |
| 5 | ✅ 완료 | Agent Layer (Unityctl.Mcp MCP 서버, schema, exec, workflow) |
| MCP Hybrid | ✅ 완료 | unityctl_run (allowlist 59 write 명령) + schema filter |
| Write C | ✅ 완료 | 커버리지 확장 (Asset 6 + Prefab 4 + Package/Settings 5 + Material 4 + Animation/UI 5 + Scene 2 + History 2 = 28개) |
| Script v1 | ✅ 완료 | script create/edit/delete/validate |
| Diagnostics | ✅ 완료 | doctor + IPC 자동 진단 |
| Read API P0 | ✅ 완료 | asset/gameobject/component + hierarchy + build-settings + reference-graph |
| P3 Screenshot | ✅ 완료 | screenshot capture (Scene/Game View, base64) |
| Build Profile | ✅ 완료 | build-profile list/get-active/set-active + build-target switch |
| P2 Batch | ✅ 완료 | batch execute + Undo transaction rollback |
| Tags & Layers | ✅ 완료 | tag/layer/console/define-symbols 10개 명령 |
| Lighting & NavMesh | ✅ 완료 | lighting 5개 + navmesh 3개 = 8개 명령, 비동기 bake 폴링 |

---

## Phase 2B 실제 반영 내용

- Plugin `PipeNameHelper.cs` 추가
- Plugin `MessageFraming.cs` 추가
- Core `MessageFraming.cs` 추가
- Plugin `IpcServer.cs` 구현
- Core `IpcTransport.cs` 구현
- `UnityctlBootstrap`에서 IPC 서버 시작
- `CommandExecutor` probe-first IPC 활성화
- `CreateIpcClientStream()` 3개 플랫폼 구현
- IPC 테스트 추가
- Linux 기본 빌드 출력명 `Game.x86_64` 보정

Phase 2B 구현 범위는 현재 코드 기준으로 아래처럼 보는 것이 정확합니다.

- 완료:
  - `ping`
  - `status`
  - `check`
  - `test`의 "started asynchronously" 의미 반환
  - 열린 Editor `build` 요청이 실제 BuildHandler까지 도달함
  - batch fallback
- 아직 후속 보강 필요:
  - domain reload 후 자동 IPC 복구를 더 강하게 재현/검증

---

## Phase 2C 실제 반영 내용

- `StatusCode.Accepted = 104` — Shared + Plugin 동기화
- `WellKnownCommands.TestResult = "test-result"` — Shared + Plugin 동기화
- Plugin `AsyncOperationRegistry.cs` — single-flight guard + age-check (360s) + TTL prune
- Plugin `TestHandler.cs` — `Accepted` 반환, `TestResultCollector` (ICallbacks + IErrorCallbacks, leaf-only)
- Plugin `TestResultHandler.cs` — `test-result` 멱등 조회 핸들러
- Plugin `UnityctlBatchEntry.cs` — Accepted 감지 → EditorApplication.update 폴링
- Plugin `UnityctlBootstrap.cs` — 60초 주기 Prune 훅
- CLI `AsyncCommandRunner.cs` — delegate 주입 폴링 (500ms 초기 → 1s 간격)
- CLI `TestCommand.cs` — `--no-wait`, `--timeout`, PlayMode 경고
- CLI `ConsoleOutput.cs` — `ACCEPTED [104]` Cyan 출력 분기
- CLI `Program.cs` — `test` 등록에 `noWait`, `timeout` 파라미터 반영
- `CommandCatalog` — Test에 `wait`, `timeout` 파라미터 추가
- 신규 테스트 `AsyncCommandRunnerTests.cs` (7개)
- CLI csproj `ConsoleAppFramework PrivateAssets=all` (test 참조 시 source generator 충돌 방지)

---

## 검증 현황

### 자동화 검증

기준: 2026-03-18 재실행

```bash
dotnet build unityctl.slnx
dotnet test unityctl.slnx
```

| 프로젝트 | 테스트 수 | 상태 |
|----------|----------|------|
| Unityctl.Shared.Tests | 60 | ✅ |
| Unityctl.Core.Tests | 96 | ✅ |
| Unityctl.Cli.Tests | 193 | ✅ |
| Unityctl.Mcp.Tests | 16 | ✅ |
| Unityctl.Integration.Tests | 23 | ✅ |

**테스트 인벤토리 합계 388개** (scene open/create + undo/redo CLI 포함)

### robotapp 수동 검증

기준 프로젝트: `C:\Users\gmdqn\robotapp`

1. `unityctl init --project C:\Users\gmdqn\robotapp`
   - `Packages/manifest.json`에 `com.unityctl.bridge` 추가 확인
2. Unity Editor 열린 상태에서 `status`
   - 성공, `Ready`
3. Unity Editor 열린 상태에서 `ping`
   - 성공, `pong`
4. Unity Editor 열린 상태에서 `check`
   - 성공, `Compilation check passed`
5. Unity Editor 열린 상태에서 `test --mode edit`
   - 의도대로 `Busy`, `"Tests started asynchronously..."`
6. Unity Editor 열린 상태에서 `build`
   - IPC timeout이 아니라 실제 build 응답 확인
   - 현재 `robotapp` 컴파일 에러로 실패
7. Unity 재시작 후 `ping/status`
   - IPC 복구 확인
8. Unity 미실행 상태에서도 batch fallback 경로 동작 확인

현재 이 환경에서 안전하게 말할 수 있는 것은:

- IPC 경로로 `status/check/test-start`가 실제 응답함
- IPC 경로로 `ping/status/check/test-start`가 실제 응답함
- 열린 Editor `build`가 실제 BuildHandler 응답까지 도달함
- `dotnet build` / `dotnet test`가 통과함
- `init`가 외부 Unity 프로젝트에 올바른 plugin source를 기록함
- warmed state에서 `status` IPC 왕복은 대체로 `148~174ms` 수준
- warmed state에서 `ping` IPC 왕복은 대체로 `147~192ms` 수준

아직 추가 검증이 필요한 것은:

- domain reload 후 자동 IPC 복구를 더 확실하게 재현하고 닫는 일
- batch worker가 IPC 서버를 절대 띄우지 않는지에 대한 명시적 로그 검증
- pure transport-only latency를 프로세스 시작 오버헤드와 완전히 분리한 측정

### 2026-03-18 Codex 벤치마크/헤드리스 추가 검증

기준 프로젝트:

- 성능/토큰: `C:\Users\ezen601\Desktop\Jason\robotapp2`
- headless batch: `C:\Users\ezen601\Desktop\Jason\20260309`

추가로 확인된 내용:

- clean-state 재실행 기준 median latency
  - `ping`: `dotnet run 2015ms`, `published exe 304ms`, `Unityctl.Mcp 100ms`, `CoplayDev 1ms`
  - `editor_state`: `2095ms`, `303ms`, `100ms`, `100ms`
  - `active_scene`: `2094ms`, `304ms`, `99ms`, `100ms`
  - `diagnostic`: `2053ms`, `401ms`, `101ms`, `100ms`
- token efficiency
  - `unityctl schema --format json`: `11,927 B`
  - `Unityctl.Mcp tools/list`: `5,024 B`
  - `CoplayDev tools/list`: `45,705 B`
  - `Unityctl.Mcp` 기준 스키마 크기는 CoplayDev 대비 약 `9.1x` 작음
- CoplayDev `tools/list`에는 direct build tool이 없음
- headless batch (`20260309`, Editor closed)
  - `check`: 성공 (`40 assemblies`)
  - `test --mode edit`: 성공 (`18 passed`)
  - `build --dry-run`: 실패 (`Unity exited with code 1073741845`)
  - `status`: 실패 (`Unity exited with code 1073741845`)

해석:

- `unityctl`의 핵심 주장인 "headless meaningful work"는 `check`와 `EditMode test`까지는 실측으로 뒷받침된다.
- `build --dry-run` headless는 아직 모든 프로젝트에서 일반화할 수 없다.
- resident mode (`Unityctl.Mcp`)는 warmed `editor_state`/`active_scene` 기준 CoplayDev와 사실상 동등한 100ms대다.

### 2026-03-18 검증 인프라 보강

- `tests/Unityctl.Mcp.Tests/McpBlackBoxTests.cs`
  - built `unityctl-mcp.exe` 기준 stdio MCP black-box 검증 추가
  - `initialize`, `tools/list`, `unityctl_schema`, invalid tool, missing arg 경로 검증
  - logging env suppression (`Logging__LogLevel__*=None`) 전제
- `tests/Unityctl.Integration/SampleUnityProject/`
  - repo-contained 최소 Unity 샘플 프로젝트 추가
  - `com.unity.test-framework` + passing EditMode test 포함
- `tests/Unityctl.Integration.Tests/HeadlessBatchValidationTests.cs`
  - closed-editor `status`, `check`, `test --mode edit`, `build --dry-run` 검증 추가
  - `build --dry-run`은 structured `BuildFailed`도 정상적인 preflight 결과로 간주
- `tests/Unityctl.Shared.Tests/ExecHandlerContractTests.cs`
  - `ExecHandler`의 static get/set/method, arg conversion, chained/multiline/blocklist/parse failure contract 고정
- `.github/workflows/ci-dotnet.yml`
  - published CLI smoke (`--help`, `schema`, `tools --json`) 추가

### 2026-03-18 Write API Phase A 실측

기준 프로젝트:

- `C:\Users\ezen601\Desktop\Jason\My project`

실측 결과:

- `play start`
  - 성공, `isPlaying=true`, `isPaused=false`
- `play pause`
  - 성공, `isPlaying=true`, `isPaused=true`
- `play stop`
  - 성공, 수정 후 `isPlaying=false`, `isPaused=false`
- `player-settings get --key productName`
  - 성공, `"My project"`
- `player-settings get --key companyName`
  - 성공, `"DefaultCompany"`
- `player-settings set --key companyName --value "TestCo"`
  - 성공, read-back으로 `"TestCo"` 확인
- `player-settings set --key companyName --value "DefaultCompany"`
  - 성공, 원래 값으로 복구
- `asset refresh`
  - 초기 구현은 `Accepted` 후 timeout
  - 수정 후 `"Asset refresh scheduled"` 응답으로 변경
  - 후속 `ping`으로 IPC 재연결 확인

핵심 수정:

- `PlayerSettingsHandler.cs`에 `System.Collections.Generic` 누락 수정
- `PlayModeHandler.cs`
  - stop 시 `EditorApplication.isPaused = false` 정리
- `AssetRefreshHandler.cs`
  - refresh completion polling 대신 `scheduled` 응답 후 delayed refresh로 의미론 변경
- `AsyncCommandRunner.cs`
  - custom poll command / timeout 메시지 일반화
- `AssetCommand.cs`
  - `asset-refresh-result` polling 경로 추가 후, 최종적으로 handler 의미 변경에 맞춰 즉시 성공 응답 사용

### 2026-03-18 Write API Phase B 실측

기준 프로젝트:

- `C:\Users\ezen601\Desktop\Jason\My project`

실측 결과:

- `gameobject create`
  - 성공, `globalObjectId`, `sceneDirty`, `undoGroupName` 반환
- `gameobject rename`
  - 성공, `TestCube -> NewName`
- `gameobject move`
  - 성공, 같은 씬 내 parent 변경
- `gameobject delete`
  - 성공, fresh object 기준 삭제 확인
- `gameobject activate/deactivate`
  - 성공, alias 기준 `active=false/true` 정상
- `scene save`
  - 성공, active scene 저장
- `scene save --all`
  - 성공, dirty scene 1개 저장
- `PrefabGuard`
  - 성공, scene 내 `SM_Bld_Apartment_02` prefab instance child delete 시
    `Write operations on prefab instances are not supported in v1. Use scene objects or unpack the prefab first.`

핵심 수정:

- `GameObject*Handler.cs`, `SceneSaveHandler.cs`
  - `EditorSceneManager` namespace 오류 수정 (`UnityEditor.SceneManagement`)
- `GameObjectCommand.cs`
  - `ParseActive`에 `on/off`, `1/0`, `active/inactive` 추가
  - `Activate` / `Deactivate` alias 추가
- `Program.cs`
  - `gameobject activate`
  - `gameobject deactivate`
  등록

주의:

- `gameobject set-active --active false`는 CLI option binding UX가 매끄럽지 않아 alias 경로를 권장한다.
- Undo는 Unity Editor UI (`Ctrl+Z`)에서 아직 별도 수동 확인이 필요하다.

### 2026-03-18 Write API Phase B.5 실측

기준 프로젝트:

- `C:\Users\ezen601\Desktop\Jason\My project`

실측 결과:

- `component add`
  - 성공, `CompProbe`에 `UnityEngine.Rigidbody` 추가
  - `componentGlobalObjectId` 반환 확인
- `component set-property` (vector)
  - 성공, `Transform.m_LocalPosition = {"x":1,"y":2,"z":3}`
- `component set-property` (scalar)
  - `mass`는 실패 (`Property 'mass' not found on Rigidbody.`)
  - `m_Mass`는 성공 (`Rigidbody.m_Mass = 5`)
- `component remove`
  - 성공, `Rigidbody` 삭제 확인
- `PrefabGuard`
  - 성공, scene 내 `SM_Bld_Apartment_02` prefab instance root에 `component add` 시
    `Write operations on prefab instances are not supported in v1. Use scene objects or unpack the prefab first.`

핵심 해석:

- Phase B.5 API는 human-friendly field name보다 Unity serialized property path 기준이다.
- 따라서 문서/예제는 `mass`보다 `m_Mass`, `position`보다 `m_LocalPosition` 같은 serialized path를 우선 사용해야 한다.

---

## 실제 동작 상태

| 명령어 | 상태 | 비고 |
|--------|------|------|
| `unityctl` | ✅ | 버전 + 도움말 출력 |
| `unityctl --help` | ✅ | 전체 커맨드 표시 |
| `unityctl editor list` | ✅ | 설치된 Editor 탐지 |
| `unityctl init` | ✅ | `src/Unityctl.Plugin` 절대 경로 기록 |
| `unityctl ping` | ✅ | batch + IPC 모두 사용 가능 |
| `unityctl status` | ✅ | batch + IPC 모두 검증 |
| `unityctl check` | ✅ | `isCompiling` / `scriptCompilationFailed` 반영 |
| `unityctl test` | ✅ | IPC 폴링으로 실제 결과 수집, `--no-wait`/`--timeout` 지원 |
| `unityctl build` | ⚠️ | 열린 Editor transport는 확인됨, 현재 `robotapp` 컴파일 에러로 build 실패 |
| `unityctl tools` | ✅ | `CommandCatalog` 기반 메타데이터 출력 |
| `unityctl tools --json` | ✅ | 단일 catalog 기반 JSON discovery 출력 |

---

## 현재 코드 상태에서 주의할 점

- Plugin 코드는 `dotnet build`로 컴파일되지 않으므로 Unity Editor 확인이 필요합니다.
- `Shared`와 `Plugin/Shared`는 물리적으로 통합하지 않았습니다. 프로토콜 변경 시 양쪽 동기화가 필요합니다.
- `TestHandler`는 Phase 2C에서 `Accepted` + polling 모델로 실제 결과 수집을 완료했습니다.
- PlayMode 테스트는 domain reload로 콜백 소실 위험 → `--wait` 강제 비활성화됩니다. PlayMode completion handoff는 Phase 3A 범위입니다.
- `CheckHandler`는 실제 compiler errors 존재 여부는 반영하지만, 개별 에러 목록을 구조화해서 반환하지는 않습니다.

---

## Phase 3C 실제 반영 내용

- CLI `WatchCommand.cs` — `unityctl watch` 커맨드, 채널 구독 + Push 스트리밍 수신
- Plugin `WatchEventSource.cs` — ConcurrentQueue 기반 Unity 이벤트 수집, IPC Push 전송
- Plugin `EventEnvelope.cs` — Watch 이벤트 프레이밍 모델 (Shared 동기화)
- Core `IpcTransport` — Watch 스트리밍 수신 지원 (영구 파이프 연결)
- Core `MessageFraming` — Watch 스트리밍 프레임 지원
- Plugin `IpcServer` — Watch 클라이언트 연결 관리
- CLI `Program.cs` — `watch` 커맨드 등록
- Shared `WellKnownCommands` — Watch 커맨드 상수 추가
- Shared `CommandCatalog` — Watch 커맨드 메타데이터 추가
- 신규 테스트: `WatchCommandTests.cs`, `IpcTransportWatchTests.cs`
- `CommandCatalogTests` 업데이트

---

## Phase 4B 실제 반영 내용

- Shared `SceneSnapshot.cs` — 씬 스냅샷 프로토콜 모델 (SceneSnapshot, GameObjectSnapshot, ComponentSnapshot)
- Shared `SceneDiffResult.cs` — diff 결과 프로토콜 모델 (SceneDiffResult, GameObjectDiff, ComponentDiff, PropertyDiff)
- Shared `WellKnownCommands` — `SceneSnapshot`, `SceneDiff` 커맨드 상수 추가
- Shared `CommandCatalog` — Scene 커맨드 메타데이터 등록
- Shared `JsonContext` — SceneSnapshot, SceneDiffResult 직렬화 컨텍스트 추가
- Plugin `SceneSnapshotHandler.cs` — SerializedObject 순회 + GlobalObjectId 배치 API 기반 스냅샷 캡처
- Plugin `SceneDiffHandler.cs` — propertyPath 기반 diff (GlobalObjectId 매칭, epsilon float 비교)
- CLI `SceneCommand.cs` — `unityctl scene snapshot`, `unityctl scene diff` 커맨드
- CLI `Program.cs` — scene 커맨드 등록
- 신규 테스트: `SceneSnapshotTests.cs` (Shared), `SceneCommandTests.cs` (Cli)
- `CommandCatalogTests` 업데이트

검증:

- `dotnet build unityctl.slnx` 통과 (경고 0)
- `dotnet test unityctl.slnx` 통과 (261개)

---

## Phase 5 실제 반영 내용

- `src/Unityctl.Mcp/` 프로젝트 신설 — ModelContextProtocol C# SDK v1.1.0, stdio transport
- MCP 서버 12개 tool name (11 classes): ping, status, check, build, test, log, session, watch, scene-snapshot, scene-diff, schema, exec
- CLI `SchemaCommand.cs` — `unityctl schema --format json` (CommandSchema 기계 판독 스키마)
- CLI `ExecCommand.cs` — `unityctl exec --project <path> --code <expr>` (C# 식 IPC 실행)
- CLI `WorkflowCommand.cs` — `unityctl workflow run <file>` (순차 실행, continueOnError)
- Plugin `ExecHandler.cs` — Reflection 기반 C# 식 실행 핸들러
- Shared `CommandSchema.cs` — 스키마 프로토콜 모델
- Shared `WorkflowDefinition.cs` — 워크플로 정의 모델
- Shared `WellKnownCommands` — Schema, Exec, Workflow 상수 추가
- Shared `CommandCatalog` — 3개 신규 커맨드 정의
- Shared `JsonContext` — CommandSchema, WorkflowDefinition, WorkflowStep, EventEnvelope[] 등록
- Plugin `WellKnownCommands` — Exec 동기화
- CLI `Program.cs` — schema, exec, workflow 커맨드 등록
- 신규 테스트: `SchemaCommandTests.cs`, `ExecCommandTests.cs`, `WorkflowCommandTests.cs`, `CommandSchemaTests.cs`, `SchemaIntegrationTests.cs`, `Unityctl.Mcp.Tests/`

검증:

- `dotnet build unityctl.slnx` 통과 (경고 0)
- 현재 환경에서 `dotnet test unityctl.slnx -c Release` exit 0 확인

---

## MCP 하이브리드 전략 실제 반영 내용

- `src/Unityctl.Mcp/Tools/RunTool.cs` — `unityctl_run` MCP 도구 (allowlist 12개 write 명령, parameters JSON 파싱)
- `src/Unityctl.Mcp/Tools/SchemaTool.cs` — `command` 파라미터 추가 (단일 명령 스키마 온디맨드 조회)
- `tests/Unityctl.Mcp.Tests/ToolAnnotationTests.cs` — RunTool 추가, 도구 수 13개
- `tests/Unityctl.Mcp.Tests/McpBlackBoxTests.cs` — `unityctl_run` 포함 13개 도구 + 5개 신규 테스트

검증:

- `dotnet build unityctl.slnx` 통과 (경고 0)
- `dotnet test unityctl.slnx` 통과 (356개: Shared 60 + Core 96 + Cli 184 + Mcp 16)
- Unity 실측 (`My project`): create → component-add → rename → set-property → remove → delete → scene-save E2E 확인

---

## Write API Phase C 실제 반영 내용

- Shared `WellKnownCommands.cs` — 23개 상수 추가 (AssetCreate~UiSetRect)
- Shared `CommandCatalog.cs` — 23개 CommandDefinition 추가, All[] 확장 (31→54개)
- Plugin `WellKnownCommands.cs` — Shared와 동기화 (23개 상수)
- Plugin 23개 핸들러 신규 (`Editor/Commands/`):
  - C-1: AssetCreateHandler, AssetCreateFolderHandler, AssetCopyHandler, AssetMoveHandler, AssetDeleteHandler, AssetImportHandler
  - C-2: PrefabCreateHandler, PrefabUnpackHandler, PrefabApplyHandler, PrefabEditHandler
  - C-3: PackageListHandler, PackageAddHandler, PackageRemoveHandler, ProjectSettingsGetHandler, ProjectSettingsSetHandler
  - C-4: MaterialGetHandler, MaterialSetHandler, MaterialSetShaderHandler
  - C-5: AnimationCreateClipHandler, AnimationCreateControllerHandler, UiCanvasCreateHandler, UiElementCreateHandler, UiSetRectHandler
- CLI 7개 파일 (AssetCommand.cs 수정 +6 메서드, PrefabCommand.cs/PackageCommand.cs/ProjectSettingsCommand.cs/MaterialCommand.cs/AnimationCommand.cs/UiCommand.cs 신규)
- CLI `Program.cs` — 23개 app.Add() 등록
- MCP `RunTool.cs` — allowlist 12→35개 확장
- Tests `CommandCatalogTests.cs` — 이름 배열 23개 추가

검증:

- `dotnet build unityctl.slnx` 통과 (경고 0)
- `dotnet test` 기준 이번 턴 검증: Shared 60 / Core 96 / Cli 193 / Mcp 16 green
- `Integration.Tests`는 샘플 프로젝트 lock 환경에서 headless 배치 4건 실패 가능
- `scene open/create`, `undo/redo`는 Unity 실기 검증 완료
- ⚠️ Asset/Prefab/Package/Material/Animation/UI 등 다수 Phase C 명령은 여전히 Unity 실기 검증 미완

CoplayDev 대비 추정 대체율:

- AI 에이전트 일상 작업: **high-80s%** (추정, scene/undo 실측 반영)
- CoplayDev 기능 패리티: **~60%** (추정)
- 상세 비교: `docs/status/PROJECT-STATUS.md` 참조

### 2026-03-18 scene / undo / redo 실측

기준 프로젝트:

- `C:\Users\ezen601\Desktop\Jason\robotapp2`

실측 결과:

- `scene create --path Assets/Scenes/CodexValidationScene.unity --template empty`
  - 성공, 새 씬 생성 및 active scene 전환 확인
- dirty scene 상태에서 `scene open --path Assets/Scenes/Main.unity`
  - 성공적으로 structured rejection
  - 메시지: `Dirty loaded scenes exist...`
- `scene open --path Assets/Scenes/Main.unity --force`
  - 성공, `Main.unity` active scene 전환 확인
- `scene open --path Assets/Scenes/CodexValidationScene.unity --force`
  - 성공, 임시 씬으로 복귀 확인
- `gameobject create --name ValidationProbe2`
  - 성공, `scene snapshot`에서 object 존재 확인
- `undo`
  - 성공, `scene snapshot`에서 `ValidationProbe2` 제거 확인
- `redo`
  - 성공, `scene snapshot`에서 `ValidationProbe2` 복원 확인

정리:

- `scene open/create`는 코드/단위테스트뿐 아니라 Unity IPC 실기 검증까지 완료
- `undo/redo`도 실제 scene contents 기준으로 작동 확인
- 검증 후 활성 씬은 `Assets/Scenes/Main.unity`로 복구했고, 임시 씬 asset은 삭제했다

---

## 슬라이스 이력 (CLAUDE.md에서 아카이브)

아래 항목들은 CLAUDE.md "최근 확정 사항"에서 이동한 것입니다.

- Write API 전체 구현 완료 (2026-03-18): Phase A (play, player-settings, asset refresh) + Phase B (gameobject CRUD, scene save) + Phase B.5 (component add/remove/set-property). 351개 dotnet 테스트 통과. Unity 실측 완료.
- Phase 5 Agent Layer 구현 완료 (2026-03-18): Unityctl.Mcp (MCP 서버, 13개 도구), SchemaCommand, ExecCommand, WorkflowCommand, ExecHandler(Plugin). 356개 dotnet 테스트 통과
- Phase 4B Scene Diff 구현 완료 (2026-03-18): SceneSnapshotHandler, SceneDiffHandler, SceneCommand, SceneSnapshot/SceneDiffResult 프로토콜.
- Phase 3C Watch Mode 구현 완료 (2026-03-18): WatchCommand, WatchEventSource, EventEnvelope, IPC Push 스트리밍
- Phase 4A Ghost Mode 구현 완료 (2026-03-18)
- Phase 3A Session Layer 구현 완료 (2026-03-18)
- Phase 3B Flight Recorder 구현 완료 (2026-03-18)
- Phase 2C Async Commands 구현 완료 (2026-03-18)
- Phase 2B 후속 검증 종결 (2026-03-18): IPC 도메인 리로드 자동 복구 실측 완료 (20회 연속 ping 무실패), batch worker IPC silent skip 확인, transport latency 측정 (ping median 528ms, MCP resident 100ms).
- Schema 정합성 + Material Create (2026-03-18): schema에 cliName/cliFlag 필드 추가. material create 명령 추가. allowlist 40개.
- Write API 확장 구현 완료 (2026-03-18): 28개 신규 write 명령. 총 40개 write/action 명령. MCP 도구 13개 유지. 388개 dotnet 테스트.
- MCP 하이브리드 전략 구현 완료 (2026-03-18): unityctl_run (allowlist 12개 write 명령), unityctl_schema(command=...) 온디맨드 필터. MCP 도구 12→13개. 356개 dotnet 테스트 통과.
- Script Editing v1 + Doctor 명령 (2026-03-19): script create/edit/delete/validate 4개 명령 구현. unityctl doctor 진단 명령 추가.
- P0 잔여분 완료 (2026-03-19): asset get-labels, asset set-labels, build-settings set-scenes 3개 명령 추가. MCP 도구 24개.
- Lighting & NavMesh (2026-03-19): lighting bake(비동기)/cancel/clear/get-settings/set-settings + navmesh bake/clear/get-settings 8개 명령. 비동기 bake는 IpcOnlyAsyncCommandRunner + Lightmapping.isRunning 폴링. MCP 도구 30개. 491개 dotnet 테스트 통과.

---

## 라이브 검증 아카이브

아래 검증 기록은 PROJECT-STATUS.md에서 이동한 것입니다. 최신 검증은 PROJECT-STATUS.md를 참조하세요.

### 기본 기능 (robotapp2, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| `ping` | ✅ | IPC + batch 모두 동작 |
| `status` | ✅ | isCompiling, isPlaying, platform 정상 |
| `check` | ✅ | 44 assemblies, scriptCompilationFailed 정상 |
| `build --dry-run` | ✅ | 19개 preflight 항목 검증 |
| `build` (실제) | ✅ | 프로젝트 에러 정확히 캡처 |
| `test --mode edit` | ✅ | 410개 실행, 403 pass / 7 fail |
| `exec --code` | ✅ | IPC로 C# 식 실행 |
| `schema --format json` | ✅ | 전체 커맨드 스키마 JSON 출력 |
| `session list` | ✅ | 세션 추적 + 기록 정상 |
| `log --stats` | ✅ | NDJSON 로그 기록/쿼리 정상 |
| `scene snapshot` | ✅ | 동작 확인 |
| `watch --channel console` | ✅ | IPC 스트리밍 동작, heartbeat 수신 확인 |
| `editor list` | ✅ | 설치된 에디터 자동 탐색 |
| `init` | ✅ | manifest.json 플러그인 설치 |

### Write API (My project, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| `play start/pause/stop` | ✅ | isPlaying/isPaused 정상 |
| `player-settings get/set` | ✅ | productName, companyName read/write |
| `asset refresh` | ✅ | scheduled 응답 + IPC 재연결 확인 |
| `gameobject create/rename/move/delete` | ✅ | GlobalObjectId, Undo, PrefabGuard |
| `gameobject activate/deactivate` | ✅ | alias 기반 |
| `component add/set-property/remove` | ✅ | vector/scalar, serialized path 기준 |
| `scene save/save --all` | ✅ | dirty scene 저장 |
| `scene create/open` | ✅ | dirty scene 보호 + --force |
| `undo/redo` | ✅ | scene snapshot으로 제거/복원 확인 |
| `PrefabGuard` | ✅ | structured rejection 확인 |
| `batch execute` 성공 | ✅ | 1회 IPC 왕복 + undo 1회 함께 제거 |
| `batch execute` 실패 rollback | ✅ | rolledBack=true, 후속 find 0건 |

### Read API (My project, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| `scene snapshot --include-inactive` | ✅ | 기본 6개, includeInactive 7개 |
| `gameobject find/get` | ✅ | componentTypes[], summary 반환 |
| `component get` | ✅ | serialized properties, NotFound error |
| `asset find` | ✅ | t:Scene 6건, t:Material 83건 |
| `asset get-info/get-dependencies` | ✅ | guid/type/labels, recursive |
| `scene hierarchy` | ✅ | nested tree, includeInactive |
| `build-settings get-scenes` | ✅ | SampleScene.unity, enabled/order |
| `asset reference-graph` | ✅ | direct/transitive 확인 |

### Screenshot (My project, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| `screenshot capture --view scene/game` | ✅ | PNG/JPG base64, 커스텀 해상도 |
| `screenshot capture --output` | ✅ | 345KB PNG 파일 생성 |

### 경쟁 분석 / CoplayDev 대비 대체율

| 관점 | 추정 대체율 | 비고 |
|------|-----------|------|
| AI 에이전트 일상 작업 | **high-80s%** | GO/Component/Asset/Prefab/Material/Scene/Play/Test/Build 커버 |
| CoplayDev 기능 패리티 | **~60%** | CoplayDev 39개 도구 중 ~60% 대응 |
| unityctl 독점 기능 포함 | **+25%p** | build/dry-run, flight recorder, session, watch, scene diff, headless batch |

### unityctl 독점 기능 (CoplayDev 대체율 0%)

| 기능 | 가치 |
|------|------|
| Headless Batch Mode | CI/CD에서 Editor 없이 status/check/test/build |
| Ghost Mode (dry-run) | 빌드 전 3단계 preflight 검증 |
| Flight Recorder | 전 커맨드 NDJSON 감사 로그 |
| Session Layer | 6개 상태머신 기반 실행 추적 |
| Watch Streaming | IPC Push 기반 실시간 이벤트 |
| Scene Diff | SerializedObject propertyPath diff |
| 토큰 효율 | 스키마 크기 ~8.3x 절감 |
| Undo 내부 통합 | UndoScope 기반 + undo/redo CLI 노출 |

### Plugin 호환성 수정 (Unity 6)

- `WatchEventSource`: `CompilationFinishedHandler` → `Action<object>` (Unity 6 API 변경)
- `WatchEventSource`: `EditorApplication.CallbackFunction` → `Action`
- `WatchEventSource.Subscribe`: 메인 스레드로 이동 (`EditorApplication.delayCall`)
- `IpcServer`: `Environment.TickCount64` → `(long)Environment.TickCount` (Mono 호환)
- `IpcServer.WatchWriterLoop`: 즉시 heartbeat 전송 (연결 안정성)

---

## 다음 단계

1. macOS / Linux 실제 테스트
2. `dotnet tool` NuGet 패키지 배포
3. write API property alias 개선 (`mass` → `m_Mass`)
4. Phase 1C 잔여 — `release.yml`, README 정비

---

## 기술 결정 로그

| 날짜 | 결정 | 이유 |
|------|------|------|
| 2026-03-17 | .NET 10 유지 | 현재 개발 머신과 테스트 환경이 .NET 10 기준으로 안정화됨 |
| 2026-03-17 | `CommandCatalog` 도입 | CLI 등록 정보와 `tools --json` drift 제거 |
| 2026-03-17 | `CommandRunner` 도입 | CLI command boilerplate 공통화 |
| 2026-03-17 | `PluginSourceLocator` 도입 | `init`의 하드코딩 경로 제거 |
| 2026-03-17 | `UnityVersionComparer` 도입 | 문자열 정렬 대신 버전 파싱 기반 선택 |
| 2026-03-17 | `test`는 2B에서 비동기 시작 의미만 반환 | 즉시 성공으로 오해되는 동작 제거, 실제 완료는 2C로 분리 |
| 2026-03-17 | IPC는 probe-first | probe 실패 시에만 batch 폴백해 중복 실행 방지 |
| 2026-03-18 | Polling 모델 채택 (Phase 2C) | IPC listen loop 직렬 구조상 장기 점유 차단 방지, 핸들러 메인 스레드 데드락 방지 |
| 2026-03-18 | Single-flight guard + age-check | 콜백 혼선 방지 (360s maxAge, Running TTL 10분) |
| 2026-03-18 | PlayMode --wait 강제 비활성화 | domain reload로 static registry 초기화 + 콜백 소실 |
| 2026-03-18 | ConsoleAppFramework PrivateAssets=all | 테스트 프로젝트 참조 시 source generator 타입 충돌 방지 |
