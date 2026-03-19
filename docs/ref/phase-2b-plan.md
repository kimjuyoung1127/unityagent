# Phase 2B: IPC Transport

## 상태

**구현 완료.**

이 문서는 이제 "구현 플랜"이라기보다, 실제 구현 범위와 후속 보강 포인트를 함께 정리하는 기준 문서입니다.

---

## 목표

기존 batch transport만으로는 Unity를 새로 스폰해야 해서 응답이 보통 수십 초 걸립니다.

Phase 2B의 목표는 실행 중인 Unity Editor와 직접 통신하는 IPC 경로를 추가하는 것이었습니다.

- Windows: Named Pipe
- macOS / Linux: .NET `NamedPipe*Stream`의 Unix Domain Socket 구현 사용

현재 코드 기준으로 달성된 핵심은 아래와 같습니다.

- 열린 Editor에 `status` / `check` / `test-start`를 빠르게 전달
- `CommandExecutor`가 IPC 우선, batch 폴백을 자동 선택
- Phase 2C와 3C를 위한 기본 transport 토대 확보

주의:

- `<200ms`는 여전히 **best-effort** 목표입니다
- `EditorApplication.update` 기반 메인 스레드 디스패치라 hard guarantee는 아닙니다

---

## 실제 구현 범위

### 반영 완료

- Plugin `PipeNameHelper.cs`
- Plugin `MessageFraming.cs`
- Core `MessageFraming.cs`
- Plugin `IpcServer.cs`
- Core `IpcTransport.cs`
- `UnityctlBootstrap`의 batchmode 가드 + IPC 서버 시작
- `CommandExecutor`의 probe-first 전략
- 3개 플랫폼 `CreateIpcClientStream()`
- IPC 관련 Core 테스트
- Linux build 출력명 `Game.x86_64`

### 현재 사용자 관점에서 검증된 명령

- `ping`
- `status`
- `check`
- `test --mode edit`
  - 단, 현재 의미는 "started asynchronously"이며 완료 결과가 아님

### 아직 후속 작업이 필요한 항목

- 도메인 리로드 후 IPC 자동 복구 검증
- batch worker가 IPC 서버를 띄우지 않는다는 점의 명시적 로그 검증
- pure transport-only latency 분리 측정

---

## 구현 세부

### 1. Pipe name

Plugin은 `Unityctl.Shared`를 직접 참조하지 못하므로, `Constants.GetPipeName()` 알고리즘을 Plugin 쪽 `PipeNameHelper`에 복제했습니다.

현재 기준:

- path normalization 일치
- SHA256 기반 결정적 이름 생성
- Plugin / Core 간 알고리즘 불일치 없음

### 2. Message framing

양쪽 모두 4-byte little-endian 길이 + UTF-8 JSON framing을 사용합니다.

이 선택의 이유:

- Windows named pipe byte/message mode 차이에 덜 의존적
- Linux/macOS와 같은 wire format 유지
- partial read/write 처리 명확화

### 3. IPC server

Plugin `IpcServer`는 아래 구조로 동작합니다.

- background thread에서 연결 수락
- request 읽기
- main thread queue로 dispatch
- `EditorApplication.update`에서 handler 실행
- 응답 직렬화 후 pipe write

현재 구현에는 아래 안전 장치가 들어가 있습니다.

- `Application.isBatchMode` 가드
- `Start()` idempotent
- probe-first 사용
- `SendAsync` 실패 후 batch 재실행 금지

### 4. Transport selection

현재 `CommandExecutor`는 아래 순서로 동작합니다.

1. `IpcTransport.ProbeAsync()`
2. probe 성공 시 `SendAsync()`
3. probe 실패 시에만 batch fallback

이 정책은 요청이 이미 Editor에 도달한 뒤 응답만 놓친 경우의 중복 실행을 막기 위한 것입니다.

---

## 검증 결과

### 자동화

2026-03-17 기준:

```bash
dotnet build unityctl.slnx
dotnet test unityctl.slnx
```

결과:

- build 통과
- test 통과
- 총 78개 테스트 통과

### 수동

기준 프로젝트: `C:\Users\gmdqn\robotapp`

검증 완료:

1. `unityctl init --project C:\Users\gmdqn\robotapp`
2. Unity Editor 열린 상태에서 `ping --json`
3. Unity Editor 열린 상태에서 `status --json`
4. Unity Editor 열린 상태에서 `check --json`
5. Unity Editor 열린 상태에서 `test --mode edit --json`
6. Unity Editor 열린 상태에서 `build --json`
7. Unity 재시작 후 `ping --json` / `status --json` 회복 확인
8. Unity 미실행 상태 batch fallback

확인된 동작:

- `ping`는 `pong`
- `status`는 `Ready`
- `check`는 `Compilation check passed`
- `test`는 `Busy` + `"Tests started asynchronously..."`
- `build`는 IPC timeout이 아니라 실제 build 응답을 반환했고, 현재 실패 원인은 `robotapp`의 컴파일 에러
- Unity 재시작 후 `ping/status`는 다시 정상 응답
- batch fallback은 정상 동작

---

## 현재 남은 약점

### 1. domain reload 자동 복구 종결 검증 미완

이전 세션에서 domain reload 직후 IPC가 `ProjectLocked` / `all pipe instances are busy` 상태로 흔들린 사례가 있었고, 이후 `IpcServer`에 `maxServerInstances=4`와 `ERROR_PIPE_BUSY` backoff를 넣었습니다. 다만 현재는 Unity 재시작 후 회복만 확실히 확인됐고, reload만으로 자동 회복된다는 점은 더 강한 재현이 필요합니다.

2026-03-19 추가 메모:

- listener가 요청 전체를 inline 처리하던 구조를 worker handoff 구조로 바꿨다.
- 현재는 accepted pipe를 worker thread에 넘기고, listener는 즉시 다음 `NamedPipeServerStream` 인스턴스를 다시 대기시킨다.
- `My project`에서 intentionally held pipe connection 1개를 유지한 상태에서도 후속 `ping` / `status`가 계속 IPC로 성공함을 실측했다.
- 남은 검증 포인트는 domain reload 직후와 장시간 import 중에도 같은 특성이 유지되는지다.

### 2. build 성공 여부는 transport와 프로젝트 상태를 분리해서 봐야 함

열린 Editor `build` 요청은 실제 `BuildHandler`까지 도달합니다. 현재 `robotapp`에서는 `Assets\\Scripts\\Visualization\\TargetMarkerVisual.cs`의 `AssetDatabase` 사용으로 컴파일 에러가 있어 build 자체가 실패합니다. 따라서 현재 blocker는 transport가 아니라 프로젝트 상태입니다.

### 3. latency 측정 해상도

built `unityctl.exe` 기준으로 warmed state에서는 `status`가 대략 `148~174ms`, `ping`이 대략 `147~192ms`였습니다. 다만 이 수치도 CLI 프로세스 시작 비용을 포함하므로 pure transport-only latency는 아닙니다.

---

## 후속 작업

1. 도메인 리로드 후 IPC 자동 복구를 더 강하게 재현/종결 검증
2. batch worker IPC 미기동 로그 검증
3. pure transport-only latency 분리 측정
4. Phase 2C async completion 작업 시작

---

## 관련 문서

- `docs/internal/DEVELOPMENT.md`
- `docs/status/PROJECT-STATUS.md`
- `docs/ref/phase-roadmap.md`
