# Phase 3B — Flight Recorder

## 목표
모든 커맨드 실행을 구조화된 NDJSON 로그로 기록하는 Flight Recorder 구현.
이후 Phase의 디버깅 인프라로 활용.

## 선행 조건
- Phase 2C 완료 (현재 master 기준)
- 기존 stub: `src/Unityctl.Core/FlightRecorder/FlightLog.cs`, `FlightEntry.cs`

## 산출물

### Core 수정
- `FlightEntry.cs` 확장 (기존 7필드 → 15필드):
  - 기존: ts, op, project, transport, statusCode, durationMs, requestId
  - 추가: level, exitCode, error, unityVersion, machine, v (CLI 버전), args, sid
- `FlightLog.cs` 구현:
  - `Record(FlightEntry)`: NDJSON append
  - `Query(FlightQuery)`: 필터링 + 페이징
  - `Prune()`: 보존 정책 적용 (30일 / 50MB)
  - `GetStats()`: 디렉토리 크기, 항목 수, 기간
  - 파일명: `flight-YYYY-MM-DD.ndjson`
  - 위치: `Constants.GetConfigDirectory() + "/logs/"`
  - 절대 CLI를 크래시시키지 않음 (try-catch 삼킴)
- `FlightQuery.cs` 신규: Op, Level, Since, Until, Last, ProjectPath 필터

### CLI 수정
- `Program.cs`에 `log` 커맨드 등록
- `LogCommand.cs` 신규:
  - `unityctl log` (최근 20개)
  - `unityctl log --last N`
  - `unityctl log --tail` (FileSystemWatcher follow)
  - `unityctl log --op build`
  - `unityctl log --level error`
  - `unityctl log --since 2026-03-15`
  - `unityctl log --json`
  - `unityctl log --prune`
  - `unityctl log --stats`
- `CommandRunner.cs` 수정: 실행 전후에 FlightLog.Record() 호출 (Stopwatch 타이밍)
- `AsyncCommandRunner.cs` 수정: 동일하게 Record() 호출

### Shared 수정
- `Constants.cs`에 `LogsDirectory = "logs"` 추가 (없으면)
- `CommandCatalog.cs`에 Log 커맨드 메타데이터 추가

### 테스트
- `Core.Tests/FlightRecorder/FlightLogTests.cs`:
  - Record 후 파일 생성 확인
  - NDJSON 형식 검증 (줄 단위 JSON 파싱)
  - Query 필터링 (op, level, since, last)
  - Prune 동작 (보존 정책)
  - 동시 쓰기 안전 (병렬 Record)
  - Record 실패 시 예외 삼킴 확인
- `Cli.Tests/LogCommandTests.cs`:
  - --json 출력 형식
  - --stats 출력
- `Integration.Tests/`:
  - CLI `log --stats` 실행 후 exit code 0

## 규칙
- `docs/ref/code-patterns.md` 패턴 준수
- `TreatWarningsAsErrors=true`
- `dotnet build unityctl.slnx` 경고 0
- `dotnet test unityctl.slnx` 전체 통과
- JsonContext에 신규 타입 등록
