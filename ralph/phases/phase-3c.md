# Phase 3C — Watch Mode

## 목표
Unity Editor 이벤트를 CLI에 실시간 Push 스트리밍.
기존 IPC 프로토콜 확장.

## 선행 조건
- Phase 4A 완료
- 기존 stub: `ITransport.SubscribeAsync` (IAsyncEnumerable<EventEnvelope>)
- 기존: `EventEnvelope` (Shared/Protocol/)

## 산출물

### Plugin 수정
- `IpcServer.cs` 수정:
  - `watch` 커맨드 수신 시 영구 연결 유지 (기존 request-response 대신 streaming)
  - 채널별 콜백 등록 (console, hierarchy, compilation)
  - `ConcurrentQueue<EventEnvelope>` 버퍼 (bounded 1000, drop-oldest)
  - 전용 writer 스레드에서 동기 파이프 쓰기
  - `_heartbeat` 이벤트 (5~10초 간격)
  - `_close` 이벤트 (정상 종료 시)
  - `_overflow` 합성 이벤트 (드롭 발생 시)
- `WatchEventSource.cs` 신규:
  - Console: `Application.logMessageReceivedThreaded` 구독 (lock-free enqueue만, Unity API 호출 금지)
  - Hierarchy: `EditorApplication.hierarchyChanged` 구독
  - Compilation: `CompilationPipeline.compilationStarted/Finished` 구독
  - `[ThreadStatic] static bool _inHandler` 재진입 가드 (재귀 로깅 방지)
  - `EditorApplication.update` 펌프에서 프레임당 최대 50개 dequeue → 파이프 전송

### Core 수정
- `IpcTransport.cs`: `SubscribeAsync` 구현 (기존 stub 교체)
  - subscribe 커맨드 전송 후 영구 읽기 루프
  - 연결 끊김 감지 → 자동 재연결 (지수 백오프)
  - CancellationToken 지원
- `CommandExecutor.cs`: watch 경로 추가 (IPC only, batch 미지원)

### CLI 수정
- `WatchCommand.cs` 신규:
  - `unityctl watch [channel]` (console, hierarchy, compilation, all)
  - `unityctl watch --format json` (NDJSON 출력)
  - `unityctl watch --no-color`
  - `Console.CancelKeyPress` → graceful shutdown
  - 자동 재연결 + `[reconnecting...]` 출력
  - 텍스트 모드: `[HH:mm:ss.fff] [channel/type] message` 형식
  - 컬러: error=Red, warning=Yellow, log=White

### Shared 수정
- `WellKnownCommands.cs`에 `Watch = "watch"` 추가
- `EventEnvelope.cs` 확인: channel, eventType, timestamp, sessionId, payload 필드
- `CommandCatalog.cs`: watch 커맨드 메타데이터

### 테스트
- `Core.Tests/Transport/IpcTransportWatchTests.cs`:
  - SubscribeAsync mock 테스트
  - 재연결 로직
  - CancellationToken 존중
- `Cli.Tests/WatchCommandTests.cs`:
  - 텍스트 포맷 출력 검증
  - JSON 모드 출력 검증
- (실제 Unity 스트리밍은 수동 검증)

## 리스크
- 도메인 리로드 → IPC 끊김 → CLI 자동 재연결 (이벤트 갭 감수)
- 메인 스레드 고갈 → 프레임당 50개 제한
- 다수 watcher 동시 연결 → MaxServerInstances 증가 또는 fan-out
- Plugin 동기 I/O 전용 (Unity Mono 비동기 미검증)

## 규칙
- `docs/ref/code-patterns.md` 패턴 준수
- `TreatWarningsAsErrors=true`
- Plugin 코드는 Unity API 의존 → `dotnet build`로 컴파일 불가
