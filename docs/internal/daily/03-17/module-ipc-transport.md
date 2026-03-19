# 2026-03-17 — Phase 2B IPC Transport

> Historical snapshot only.
> This file records what was true on 2026-03-17 and may not match the current codebase.
> Current source of truth: `docs/DEVELOPMENT.md`, `docs/status/PROJECT-STATUS.md`, `docs/ref/phase-2b-plan.md`

## 완료 항목
- Plugin Shared 프로토콜 타입 체크인 (4 파일 + .meta 5개)
- PipeNameHelper: SHA256 파이프명 생성 (Plugin 측)
- MessageFraming: 동기(Plugin) + 비동기(Core) length-prefixed framing
- IpcServer: Named Pipe 서버 전체 구현 (~260줄)
  - singleton, background thread, ConcurrentQueue, ManualResetEventSlim
  - EditorApplication.update 메인 스레드 디스패치
  - beforeAssemblyReload / quitting lifecycle hooks
  - batchmode 가드, idempotent Start()
- IpcTransport: Named Pipe 클라이언트 (ProbeAsync + SendAsync)
- CommandExecutor: IPC probe-first + batch 폴백 활성화
- Platform CreateIpcClientStream: Windows/Mac/Linux 3개 구현
- IPC 테스트 6개 추가 (총 65개)
- BuildHandler Linux 출력명: Game → Game.x86_64
- .gitignore: Shared~/ 라인 제거

## 변경 파일 (20개)
`.gitignore`, Plugin/Shared/ (4+5meta), Plugin/Ipc/ (3+3meta), Core/Transport/ (2), Core/Platform/ (3), Core/csproj, tests/ (1), docs/ (2)

## 블로커
- .NET SDK 10 미설치 → 빌드/테스트 실행 불가
- Unity Editor 수동 IPC 검증 미수행
