# unityctl AGENTS Index

이 저장소의 Source-of-Truth 탐색 문서입니다.
폴더 책임, 파일 탐색 순서, 작업 규칙을 여기서 파악합니다.

## Start Here
1. `AGENTS.md` (Codex) 또는 `CLAUDE.md` (Claude) — 동일 정책 진입 문서
2. `docs/ref/architecture-mermaid.md`
3. `docs/status/PROJECT-STATUS.md`
4. `docs/ref/phase-roadmap.md`

## Mandatory Navigation Rule
- 항상 루트 `AGENTS.md`를 먼저 읽은 후 파일 탐색을 시작한다.
- `src/` 하위 프로젝트 작업 시 해당 폴더의 구조를 먼저 파악한다.
- 빠른 시스템 맥락은 `docs/ref/architecture-mermaid.md`를 먼저 읽는다.
- 제품/계획 작업은 `docs/status/PROJECT-STATUS.md`와 `docs/ref/` 정식 문서를 먼저 읽는다.

## Folder Responsibility Rule

### src/Unityctl.Shared (netstandard2.1)
프로토콜 + 모델 + 상수. CLI/Core/Plugin 모두 참조하는 공유 계약.
- `Protocol/` — CommandRequest, CommandResponse, StatusCode, WellKnownCommands
- `Transport/` — ITransport, TransportCapability
- `Serialization/` — JsonContext (System.Text.Json Source Generator)
- `Models/` — UnityEditorInfo, BuildRequest, TestRequest
- `Constants.cs` — 버전, 파이프명, 타임아웃

### src/Unityctl.Core (net10.0)
비즈니스 로직. Transport, Discovery, Retry, Platform 추상화.
- `Transport/` — BatchTransport, IpcTransport, MessageFraming, CommandExecutor
- `Platform/` — IPlatformServices, Windows/Mac/Linux 구현
- `Discovery/` — UnityEditorDiscovery
- `Retry/` — RetryPolicy

### src/Unityctl.Cli (net10.0)
얇은 CLI 셸. 모든 로직은 Core에 위임.
- `Commands/` — PingCommand, StatusCommand, BuildCommand, TestCommand, CheckCommand, EditorCommands, InitCommand, ToolsCommand
- `Output/` — ConsoleOutput, JsonOutput

### src/Unityctl.Mcp (net10.0)
네이티브 MCP 서버. 얇은 도구 레이어에서 Core를 호출한다.
- `Tools/` — `unityctl_query`, `unityctl_run`, `unityctl_schema`, `unityctl_status`, `unityctl_watch` 등 top-level MCP 도구
- `Program.cs` — stdio MCP 서버 부트스트랩

### src/Unityctl.Plugin (Unity UPM)
Unity Editor 브릿지. `dotnet build` 불가 — Unity 내에서만 컴파일.
- `Editor/Ipc/` — IpcServer, IpcRequestRouter, PipeNameHelper, MessageFraming
- `Editor/Commands/` — CommandRegistry, PingHandler, StatusHandler, BuildHandler, TestHandler, TestResultHandler, CheckHandler, AsyncOperationRegistry
- `Editor/Bootstrap/` — UnityctlBootstrap (InitializeOnLoad)
- `Editor/BatchMode/` — UnityctlBatchEntry
- `Editor/Shared/` — Protocol 타입 소스 복사 (Newtonsoft 기반)

### tests/
- `Unityctl.Shared.Tests` — 프로토콜 roundtrip, typed accessor, command catalog
- `Unityctl.Core.Tests` — PipeName, RetryPolicy, IPC Transport, platform helpers
- `Unityctl.Cli.Tests` — PlatformFactory, EditorDiscovery, AsyncCommandRunner, tools metadata, command runner
- `Unityctl.Mcp.Tests` — MCP 도구 등록, black-box stdio 서버 검증
- `Unityctl.Integration.Tests` — Black-box CLI 테스트

## Authoring Rule
- Shared 수정 시 Plugin `Editor/Shared/` 소스 복사본도 동기화 필요.
- Plugin 코드는 Unity API에 의존하므로 `dotnet build`로 검증 불가.
- `TreatWarningsAsErrors=true` — 경고 0이어야 빌드 성공.
- 새 프로토콜 타입 추가 시 JsonContext에 `[JsonSerializable]` 등록 필수.

## Product Doc Governance
- 정식 참조 문서는 `docs/ref/`에만 보관.
- 운영 상태는 `docs/status/`에서만 관리.
- 상태 변경 시 관련 downstream 문서도 같은 턴에 동기화.
- 마일스톤 변경은 `docs/internal/daily/MM-DD/` 로그 + 주간 롤업 반영.

## Task Routing
1. 아키텍처/설계 확인: `docs/ref/architecture-mermaid.md`
2. 현재 Phase 상태: `docs/status/PROJECT-STATUS.md`
3. Phase별 상세 실행: `docs/status/PHASE-EXECUTION-BOARD.md`
4. 코드 컨벤션/패턴: `docs/ref/code-patterns.md`
5. IPC Transport 설계: `docs/ref/phase-2b-plan.md`
6. Phase 로드맵 전체: `docs/ref/phase-roadmap.md`
7. AI 에이전트 빠른 시작: `docs/ref/ai-quickstart.md`
8. 사용자 시작 가이드: `docs/ref/getting-started.md`
