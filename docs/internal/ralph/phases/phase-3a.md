# Phase 3A — Session Layer

## 목표
커맨드 실행 세션을 추적하는 Session Layer 구현.
MCP Tasks 대응 내부 모델. Flight Recorder 위에 구축.

## 선행 조건
- Phase 3B 완료 (FlightLog 동작)

## 산출물

### Core 신규 (`Unityctl.Core/Session/`)
- `SessionState.cs`: enum — Created, Running, Completed, Failed, Cancelled, TimedOut
- `Session.cs`: record — Id, State, ProjectPath, Command, Transport, CreatedAt (ISO 8601), UpdatedAt, PipeName, UnityPid, CliPid, Result (JsonObject?), ErrorMessage, DurationMs
- `ISessionStore.cs`: interface — Save, Get, List, Delete, Cleanup
- `NdjsonSessionStore.cs`:
  - 활성 세션: `~/.unityctl/sessions/active.json` (덮어쓰기)
  - 완료 이력: `~/.unityctl/sessions/history.ndjson` (append)
  - TTL 자동 정리 (7일)
- `SessionManager.cs`:
  - Start(command, project) → Session (Created → Running)
  - Complete(sessionId, result) → Running → Completed
  - Fail(sessionId, error) → Running → Failed
  - Cancel(sessionId) → Running/Created → Cancelled
  - Timeout(sessionId) → Running → TimedOut
  - List(filter?) → active + recent history
  - Stale 감지: CliPid 생존 체크 (Process.GetProcessById)

### CLI 수정
- `Program.cs`에 `session` 서브커맨드 등록
- `SessionCommand.cs`:
  - `unityctl session list [--json]`
  - `unityctl session stop <id>`
  - `unityctl session clean` (stale 세션 정리)
- `CommandRunner.cs` 수정: SessionManager.Start/Complete/Fail 연동
- `AsyncCommandRunner.cs` 수정: 동일 연동

### Shared 수정
- `SessionInfo.cs` 수정: CreatedAt를 `long` → ISO 8601 `string` 마이그레이션
- `CommandCatalog.cs`에 Session 커맨드 메타데이터

### 테스트
- `Core.Tests/Session/SessionManagerTests.cs`:
  - 상태 전이 (6개 상태 × 유효/무효 전이)
  - Stale 세션 감지 (죽은 PID)
  - TTL 정리
- `Core.Tests/Session/NdjsonSessionStoreTests.cs`:
  - Save/Get/List/Delete CRUD
  - active.json + history.ndjson 분리 확인
- `Cli.Tests/SessionCommandTests.cs`:
  - list --json 출력
- `Integration.Tests/`:
  - CLI `session list` exit code 0

## 규칙
- `docs/ref/code-patterns.md` 패턴 준수
- `TreatWarningsAsErrors=true`
- `dotnet build unityctl.slnx` 경고 0
- `dotnet test unityctl.slnx` 전체 통과
