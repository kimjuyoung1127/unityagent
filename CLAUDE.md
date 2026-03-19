# unityctl 프로젝트 인덱스

unityctl 작업 시작 시 가장 먼저 읽는 진입 문서입니다.
이 문서만 읽어도 현재 단계, 규칙, 다음 행동을 빠르게 파악할 수 있게 유지합니다.

## 저장소 경계
- Write Repo: `.` (저장소 루트 — clone 위치 무관)

## 시작 순서 (필수)
1. `AGENTS.md` (Codex) 또는 `CLAUDE.md` (Claude) — 동일 정책 진입 문서
2. `docs/ref/architecture-mermaid.md`
3. `docs/status/PROJECT-STATUS.md`
4. `docs/ref/phase-roadmap.md`
5. `docs/ref/code-patterns.md` (코드 작성 전 필수)

## 현재 상태 (2026-03-19)
- Phase 0~1B: Done
- Phase 1C (CI/CD): Done
- Phase 2A/2A+: Done (Foundation + Tools Metadata)
- Phase 2B (IPC Transport): Done
- Phase 2C (Async Commands): Done
- Phase 3B (Flight Recorder): Done
- Phase 3A (Session Layer): Done
- Phase 4A (Ghost Mode): Done
- Phase 3C (Watch Mode): Done
- Phase 4B (Scene Diff): Done
- Phase 5 (Agent Layer): Done
- Write API Phase A (PlayMode, PlayerSettings, AssetRefresh): Done
- Write API Phase B (GameObject CRUD, Scene Save): Done
- Write API Phase B.5 (Component CRUD): Done
- MCP Hybrid (unityctl_run + schema filter): Done
- Write API 확장 (Scene open/create, Undo/Redo, Phase C): Done
- Script Editing v1 (create/edit/delete/validate): Done
- Diagnostics (doctor + IPC 실패 자동 진단): Done
- P0 (Read API): Done (asset find/get-info/get-dependencies/reference-graph/get-labels/set-labels, gameobject find/get, component get, scene hierarchy, build-settings get-scenes/set-scenes)
- P3 (Screenshot/Visual Feedback): Done
- Tags & Layers + Editor Utility (tag/layer/console/define-symbols 10개 명령): Done
- Lighting & NavMesh (lighting bake/cancel/clear/get-settings/set-settings, navmesh bake/clear/get-settings 8개 명령): Done
- Physics Settings (physics get-settings/set-settings/get-collision-matrix/set-collision-matrix 4개 명령): Done
- Editor Utility 확장 (editor pause/focus-gameview/focus-sceneview 3개 명령): Done
- Script List (script list 1개 명령): Done

최근 확정 사항 (최신 3개만 표시, 전체 이력은 `docs/internal/DEVELOPMENT.md` "슬라이스 이력" 참조):
- NuGet v0.2.0 배포 완료 (2026-03-19): `dotnet tool install -g unityctl` + `unityctl-mcp`. GitHub Actions release.yml 자동 배포 파이프라인 (4플랫폼 빌드 + NuGet push + GitHub Release).
- Editor Utility 확장 + Script List (2026-03-19): editor pause/focus-gameview/focus-sceneview + script list 4개 명령. Unity 실측 8/8 통과.
- Physics Settings (2026-03-19): physics 4개 명령. DynamicsManager iterator + 32×32 collision matrix. Unity 실측 9/9 통과.

## 실행 규칙 (MUST)
1. 기존 코드/타입/유틸 우선 재사용, 중복 구현 금지
2. `src/` 폴더 구조를 모듈 Source of Truth로 사용
3. Shared 수정 시 Plugin `Editor/Shared/` 동기화 필수
4. 문서와 코드 상태가 다르면 코드/테스트 실제 상태를 우선
5. 명시 요청 없이는 임의 Git 파괴 명령 금지
6. **C# 파일 생성/수정 전에 `docs/ref/code-patterns.md`를 반드시 읽고 패턴 준수**
7. `TreatWarningsAsErrors=true` — 경고 0이어야 빌드 성공
8. Plugin 코드는 Unity API에 의존 → `dotnet build` 불가, Unity Editor에서 컴파일 확인

## Quick Commands

```bash
dotnet build unityctl.slnx                                          # 빌드
dotnet test unityctl.slnx                                           # 전체 테스트 (491+개)
dotnet test unityctl.slnx --filter "FullyQualifiedName!~Integration" # 유닛만
dotnet run --project src/Unityctl.Cli -- <command> [options]         # CLI 실행
```

## Architecture

```
unityctl.slnx
├── src/Unityctl.Shared    (netstandard2.1)  프로토콜 + 모델 + 상수
├── src/Unityctl.Core      (net10.0)         비즈니스 로직 (transport, discovery, retry)
├── src/Unityctl.Cli       (net10.0)         얇은 CLI 셸 → Core에 위임
├── src/Unityctl.Plugin    (Unity UPM)       Editor 브릿지 (솔루션 빌드에 미포함)
├── tests/*Tests           xUnit 테스트 (491+개)
└── docs/                  ref/ + status/ + daily/ + weekly/
```

**의존성 방향**: `Shared ← Core ← Cli`. Plugin은 Unity 내에서만 컴파일.

## Tech Stack

| 항목 | 값 |
|------|-----|
| 런타임 | .NET 10 (SDK 10.0.201) |
| CLI 프레임워크 | ConsoleAppFramework 5.3.3 (Cysharp, delegate 기반 등록) |
| Shared 타겟 | .NET Standard 2.1 |
| JSON (CLI/Core) | System.Text.Json + Source Generator (`JsonContext`) |
| JSON (Plugin) | Newtonsoft.Json 3.2.1 (`JsonConvert`, `JObject`) |
| 테스트 | xUnit 2.9.2 |
| Unity 최소 | 2021.3+ |
| CI | GitHub Actions (Win/Mac/Linux 매트릭스) |

## Key Design Decisions

- **Payload 타입**: `JsonObject` / `JObject` — `Dictionary<string, object?>` 사용 금지
- **파이프명**: SHA256 해시 기반 결정적 생성 (`Constants.GetPipeName()`)
- **Transport 전략**: IPC probe-first → Batch 폴백 (CommandExecutor 자동 선택)
- **IPC framing**: `[4-byte LE length][UTF-8 JSON]` — 10MB 최대
- **IPC 서버**: 동기 I/O + 백그라운드 Thread (Unity Mono 비동기 미검증)
- **Plugin ↔ Shared**: 소스 직접 복사 (타입 10개 미만)
- **batchmode 응답**: response-file 패턴 (stdout/log 오염 방지)

## Phase Status

| Phase | 상태 | 요약 |
|-------|------|------|
| 0~1B | ✅ 완료 | 골격, Plugin, CLI 기본, 핵심 기능 |
| 1C | ✅ 완료 | CI + release.yml + README |
| 2A/2A+ | ✅ 완료 | Foundation + Tools Metadata |
| 2B | ✅ 완료 | IPC Transport (Named Pipe, probe-first) |
| **2C** | ✅ 완료 | **Async Commands** (polling, single-flight, ACCEPTED) |
| **3B** | ✅ 완료 | **Flight Recorder** (NDJSON 로깅, append-only, 예외 안전) |
| **3A** | ✅ 완료 | **Session Layer** (상태머신 6개, NDJSON 저장소, MCP Tasks 매핑) |
| **4A** | ✅ 완료 | **Ghost Mode** (--dry-run preflight, 3단계 검증) |
| **3C** | ✅ 완료 | **Watch Mode** (Push 스트리밍, ConcurrentQueue, 영구 파이프) |
| **4B** | ✅ 완료 | **Scene Diff** (SerializedObject, GlobalObjectId, propertyPath diff) |
| **5** | ✅ 완료 | **Agent Layer** (Unityctl.Mcp MCP 서버, schema, exec, workflow) |
| **Write A** | ✅ 완료 | **PlayMode, PlayerSettings, AssetRefresh** (IPC write path) |
| **Write B** | ✅ 완료 | **GameObject CRUD + Scene Save** (GlobalObjectId, Undo, PrefabGuard) |
| **Write B.5** | ✅ 완료 | **Component CRUD** (add/remove/set-property, SerializedObject) |
| **MCP Hybrid** | ✅ 완료 | **unityctl_run** (allowlist — `RunTool.cs` 참조) + **schema filter** (command 파라미터) |
| **Write C** | ✅ 완료 | **커버리지 확장** (Asset 6 + Prefab 4 + Package/Settings 5 + Material 4 + Animation/UI 5 + Scene 2 + History 2 = 28개) |
| **Script v1** | ✅ 완료 | **script create/edit/delete/validate** (템플릿 생성, whole-file replace, 비동기 컴파일 검증) |
| **Diagnostics** | ✅ 완료 | **doctor** (IPC/Plugin/Editor 상태 진단) + IPC 실패 시 Editor.log 자동 진단 |
| **P0 Read API** | ✅ 완료 | **asset find/get-info/get-deps/ref-graph/get-labels/set-labels** + gameobject find/get + component get + scene hierarchy + build-settings get/set-scenes |
| **P3 Screenshot** | ✅ 완료 | **screenshot capture** (Scene/Game View, base64, PNG/JPG, MCP 전용 도구) |
| **Tags & Layers + Editor Utility** | ✅ 완료 | **tag list/add, layer list/set, gameobject set-tag/set-layer, console clear/get-count, define-symbols get/set** (10개 명령) |
| **Lighting & NavMesh** | ✅ 완료 | **lighting bake/cancel/clear/get-settings/set-settings + navmesh bake/clear/get-settings** (8개 명령) |
| **Physics Settings** | ✅ 완료 | **physics get-settings/set-settings/get-collision-matrix/set-collision-matrix** (4개 명령) |
| **Editor Utility 확장** | ✅ 완료 | **editor pause/focus-gameview/focus-sceneview** (3개 명령) |
| **Script List** | ✅ 완료 | **script list** (MonoScript 탐색, folder/filter/limit) |

## Source of Truth 문서
- 탐색 인덱스: `AGENTS.md`
- 아키텍처 (빠른 맥락): `docs/ref/architecture-mermaid.md`
- 코드 패턴: `docs/ref/code-patterns.md`
- Phase 로드맵: `docs/ref/phase-roadmap.md`
- Phase 2B 설계: `docs/ref/phase-2b-plan.md`
- 프로젝트 상태: `docs/status/PROJECT-STATUS.md`
- Phase 실행 보드: `docs/status/PHASE-EXECUTION-BOARD.md`
- 사용자 가이드: `docs/ref/getting-started.md`
- AI 빠른 시작: `docs/ref/ai-quickstart.md`
- 용어 사전: `docs/ref/glossary.md`
- 개발 진행 상세: `docs/internal/DEVELOPMENT.md`

## Task Routing
1. 아키텍처/설계 확인: `docs/ref/architecture-mermaid.md`
2. 현재 상태 확인: `docs/status/PROJECT-STATUS.md`
3. Phase별 실행 현황: `docs/status/PHASE-EXECUTION-BOARD.md`
4. 코드 작성 전 패턴: `docs/ref/code-patterns.md`
5. IPC Transport 설계: `docs/ref/phase-2b-plan.md`
6. 전체 로드맵: `docs/ref/phase-roadmap.md`
7. 개발 진행 상세 이력: `docs/internal/DEVELOPMENT.md`

## 테스트 표준
- `dotnet test unityctl.slnx` green 필수 (현재 수치는 `docs/status/PROJECT-STATUS.md` 참조)
- Integration.Tests는 AppLocker 감지 + graceful skip

## 다음 개발 로드맵
`docs/ref/phase-roadmap.md` "다음 개발 로드맵" 섹션 참조.
