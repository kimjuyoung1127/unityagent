# unityctl 전체 Phase 로드맵

> 최종 업데이트: 2026-03-18
> 목표: MCP 상위 호환 Unity 제어 체인

---

## MCP 대체 매핑

| MCP 기능 | unityctl 대응 | Phase | 상태 |
|----------|-------------|-------|------|
| Tools | CLI 커맨드 + `--json` | 0 ~ 2A+ | ✅ |
| tools/list | `unityctl tools --json` | 2A+ | ✅ |
| Resources | flight log, scene snapshot | 3B, 4B | ✅ |
| Prompts | `docs/ref/ai-quickstart.md` | 1C | ✅ |
| Tasks | Session Layer | 3A | ✅ |
| Streaming | Watch Mode | 3C | ✅ |
| Server | `Unityctl.Mcp` (C# SDK, stdio) | 5 | ✅ |
| Write via MCP | `unityctl_run` (allowlist 35 write 명령) | MCP Hybrid + Write C | ✅ |
| Elicitation | Ghost Mode preflight 결과 | 4A | ✅ |

unityctl은 MCP를 대체하는 동시에, Phase 5에서 네이티브 .NET MCP 서버를 직접 구현합니다.
Python/TypeScript bridge가 아닌 `ModelContextProtocol` C# SDK 기반.

---

## Phase 구조

```text
Phase 0   — 프로젝트 골격         ✅ 완료
Phase 0.5 — Plugin 부트스트랩     ✅ 완료
Phase 1A  — CLI 기본              ✅ 완료
Phase 1B  — 핵심 기능             ✅ 완료
Phase 1C  — 테스트 + 배포         ⚠️ 부분 완료
Phase 2A  — Foundation            ✅ 완료
Phase 2A+ — Tools Metadata        ✅ 완료
Phase 2B  — IPC Transport         ✅ 완료
Phase 2C  — Async Commands        ✅ 완료
Phase 3B  — Flight Recorder       ✅ 완료    ← 순서 변경: 3A보다 먼저
Phase 3A  — Session Layer         ✅ 완료
Phase 4A  — Ghost Mode            ✅ 완료
Phase 3C  — Watch Mode            ✅ 완료
Phase 4B  — Scene Diff            ✅ 완료
Phase 5   — Agent Layer           ✅ 완료
MCP Hybrid — unityctl_run + schema filter  ✅ 완료
Write C   — 커버리지 확장 (23개 명령)     ✅ 완료
```

### 실행 순서 변경 근거

| 변경 | 이유 |
|------|------|
| 3B → 3A | Flight Recorder는 독립적이며, 이후 Phase 디버깅 인프라로 활용. Session은 FlightLog 위에 구축하는 게 자연스러움 |
| 4A → 3C 앞으로 | Ghost Mode는 기존 BuildHandler 재활용이라 범위가 작고 빠름. Watch Mode는 IPC 스트리밍 프로토콜 확장이라 더 복잡 |

---

## Phase 2B — IPC Transport

상태: **구현 완료, 후속 보강 필요**

현재 코드에 반영된 것:

- Plugin `IpcServer`
- Core `IpcTransport`
- `PipeNameHelper`
- 양쪽 `MessageFraming`
- `UnityctlBootstrap` IPC 서버 시작
- `CommandExecutor` probe-first
- 3개 플랫폼 `CreateIpcClientStream`
- IPC 관련 Core 테스트

실제 검증된 범위:

- `dotnet build unityctl.slnx` 통과
- `dotnet test unityctl.slnx` 통과
- `robotapp`에서 열린 Editor 기준 `status` 성공
- `robotapp`에서 열린 Editor 기준 `ping` 성공
- `robotapp`에서 열린 Editor 기준 `check` 성공
- `robotapp`에서 열린 Editor 기준 `test --mode edit`가 비동기 시작 의미로 `Busy` 반환
- `robotapp`에서 열린 Editor 기준 `build` 요청이 실제 `BuildHandler`까지 도달함을 확인
- Unity 재시작 후 IPC가 다시 `ping/status`로 회복됨을 확인
- Unity 미실행 상태에서 batch fallback 동작 확인

아직 남은 후속 보강:

- 도메인 리로드 후 IPC 자동 복구에 대한 더 강한 재현/종결 검증
- batch worker에서 IPC 서버 미기동 로그 검증
- pure IPC latency를 CLI 프로세스 시작 비용과 분리한 추가 측정

상세 내용은 `docs/ref/phase-2b-plan.md`와 `docs/DEVELOPMENT.md`를 함께 봅니다.

### 2B 리스크 메모

| 리스크 | 상태 | 메모 |
|--------|------|------|
| 서버 종료 시 pending work 정리 | ✅ | shutdown completion 기반으로 보강 |
| 열린 Editor build 성공이 프로젝트 상태에 의존 | ⚠️ | transport는 검증됐지만 `robotapp` 컴파일 에러로 build 자체는 실패 |
| domain reload 자동 복구 종결 검증 미흡 | ⚠️ | 재시작 복구는 확인했지만 reload-only 자동 회복은 더 확인 필요 |
| pure latency 측정 부재 | ⚠️ | built exe 기준 warmed state는 확인했지만 transport-only 수치는 아님 |

---

## Phase 2C — Async Commands

상태: **구현 완료**

구현된 것:

- `AsyncOperationRegistry` — single-flight guard + age-check (360s) + TTL prune (Running 10분, Completed 5분)
- `TestResultCollector` — `ICallbacks` + `IErrorCallbacks` 동시 구현, leaf-only 집계 (`HasChildren` 필터)
- `TestResultHandler` — `test-result` IPC 내부 커맨드, 멱등 polling 응답
- `AsyncCommandRunner` — CLI delegate 주입 폴링 (500ms 초기 → 1s 간격)
- `TestCommand` — `--no-wait` (ConsoleAppFramework flag), `--timeout` (기본 300s)
- PlayMode + wait → 경고 + no-wait 강제
- `ConsoleOutput` — `ACCEPTED [104]` Cyan 출력 분기
- `UnityctlBatchEntry` — Accepted 감지 → `EditorApplication.update` 폴링 대기 (300s)
- `UnityctlBootstrap` — 60초 주기 `AsyncOperationRegistry.Prune` 훅
- `StatusCode.Accepted = 104`, `WellKnownCommands.TestResult = "test-result"`
- `CommandCatalog.Test`에 `wait`, `timeout` 파라미터 추가

실제 검증 (robotapp, 2026-03-18):

- `test` 기본 모드: 폴링 후 404 passed, 27.7s 소요
- `test --no-wait`: `ACCEPTED [104]` 즉시 반환
- `test --mode play`: 경고 + 즉시 반환
- `test --timeout 5`: 타임아웃 후 `TestFailed`
- single-flight: 두 번째 요청 `Busy`
- 85개 dotnet 테스트 통과

---

## Phase 3B — Flight Recorder

상태: **구현 완료**

모든 커맨드 실행을 구조화된 NDJSON 로그로 남기는 단계.
다른 Phase보다 먼저 구현하여 이후 디버깅 인프라로 활용.

### 산출물

- `FlightLog` (기존 stub 확장)
- `FlightEntry` (기존 7필드 → 15필드 확장)
- CLI: `unityctl log`

### FlightEntry 스키마

기존 필드 (유지):

| 필드 | 타입 | 설명 |
|------|------|------|
| `ts` | `long` (Unix ms) | 타임스탬프 |
| `op` | `string` | 커맨드 이름 (build, test, check 등) |
| `project` | `string?` | Unity 프로젝트 경로 |
| `transport` | `string?` | "ipc" 또는 "batch" |
| `statusCode` | `int` | StatusCode enum 값 |
| `durationMs` | `long` | 소요 시간 |
| `requestId` | `string?` | 상관 ID |

신규 추가 필드:

| 필드 | 타입 | 설명 |
|------|------|------|
| `level` | `string` | "info", "warn", "error", "fatal" |
| `exitCode` | `int` | CLI 프로세스 종료 코드 |
| `error` | `string?` | 예외 메시지 또는 에러 요약 |
| `unityVersion` | `string?` | 예: "6000.0.64f1" |
| `machine` | `string?` | `Environment.MachineName` (CI 환경 구분) |
| `v` | `string` | CLI 버전 (`Constants.Version`) |
| `args` | `string[]?` | 정제된 CLI 인자 (비밀 정보 제거) |
| `sid` | `string?` | 세션 ID (Phase 3A 연동) |

### 파일 전략

- **포맷**: NDJSON (한 줄 = 한 JSON 레코드). grep/jq 친화, append-safe, 크래시 안전
- **파일명**: `flight-YYYY-MM-DD.ndjson` (일별 파일)
- **위치**: `~/.unityctl/logs/`
- **보존**: 기본 30일 + 총 50MB 상한. 환경변수로 설정 가능
  - `UNITYCTL_LOG_RETENTION_DAYS=30`
  - `UNITYCTL_LOG_MAX_SIZE_MB=50`
- **정리**: CLI 시작 시 1회 Prune (프로세스당 1회, static flag)

### 동시 쓰기 안전

- `File.AppendAllText` 사용 (NDJSON 한 줄 < 1KB, OS 레벨 쓰기 원자성 실질 보장)
- **절대 CLI를 크래시시키지 않는다**: 모든 Record 호출을 try-catch로 감싸고 예외 삼킴

### CLI 쿼리 커맨드

```
unityctl log                          # 최근 20개
unityctl log --last 50                # 최근 50개
unityctl log --tail                   # follow 모드 (FileSystemWatcher)
unityctl log --op build               # 명령어 필터
unityctl log --level error            # 심각도 필터
unityctl log --since 2026-03-15       # 날짜 필터
unityctl log --json                   # 원본 NDJSON 출력
unityctl log --prune                  # 수동 정리
unityctl log --stats                  # 디렉토리 크기, 항목 수, 기간
```

### 참고 소스

- Java Flight Recorder (JFR) 이벤트 구조
- OpenTelemetry Logs Data Model
- Serilog 구조화 로깅 / RollingFile 패턴
- egnyte/ax (구조화 JSON 로그 쿼리 CLI)

---

## Phase 3A — Session Layer

MCP `Tasks` 대응용 내부 세션 추상화.
Flight Recorder 위에 구축하여 커맨드 실행 추적.

### 상태머신

```text
States:
  Created    — 세션 할당, 실행 전
  Running    — 커맨드 실행 중 (IPC 또는 batch)
  Completed  — 성공 완료
  Failed     — 에러 발생
  Cancelled  — 사용자 취소 (Ctrl+C 또는 session stop)
  TimedOut   — 타임아웃 초과 (Failed와 분리: 복구 액션이 다름)

Transitions:
  Created  → Running     (실행 시작)
  Running  → Completed   (성공 응답)
  Running  → Failed      (에러 응답 또는 transport 실패)
  Running  → Cancelled   (사용자 취소)
  Running  → TimedOut    (타임아웃 초과)
  Created  → Cancelled   (시작 전 취소)
```

### MCP Tasks 매핑

MCP Tasks는 draft 단계 (SEP-2229 Unsolicited Tasks, SEP-2268 Subtasks, SEP-2339 Task Continuity 진행 중).

내부 모델 먼저 구축, MCP 노출은 Phase 5에서 후행 매핑:

| unityctl | MCP Tasks |
|----------|-----------|
| `SessionId` | `taskId` |
| `Running` | `working` |
| `Completed` | `completed` |
| `Failed` | `failed` |
| `TimedOut` | `failed` (statusMessage로 구분) |
| `Cancelled` | `cancelled` |
| `CreatedAt`/`UpdatedAt` | ISO 8601 (MCP 동일) |

SEP-2339 동향: 결과를 task 객체에 인라인하는 방향 → Session에 결과 페이로드를 처음부터 포함.

### 저장소 전략

- **활성 세션**: `~/.unityctl/sessions/active.json` (소규모, 상태 변경 시 덮어쓰기)
- **완료 이력**: `~/.unityctl/sessions/history.ndjson` (append-only)
- FlightLog과 동일한 NDJSON 패턴 재사용

### 산출물

```text
Unityctl.Core/Session/
  SessionState.cs       — enum (6개 상태)
  Session.cs            — record: Id, State, ProjectPath, Command, Transport,
                                   CreatedAt, UpdatedAt, PipeName, UnityPid,
                                   Result?, ErrorMessage?, DurationMs
  ISessionStore.cs      — interface: Save, Get, List, Delete, Cleanup
  NdjsonSessionStore.cs — NDJSON 파일 구현
  SessionManager.cs     — 라이프사이클 관리: Start → track → Complete/Fail/Cancel

Unityctl.Cli/Commands/
  SessionCommand.cs     — session start, session list, session stop
```

### 주의사항

- **Stale 세션 감지**: CLI PID를 세션에 저장, `session list` 시 PID 생존 체크
- **TTL 자동 정리**: 7일 보존, `session list` 호출 시 정리
- **타임스탬프**: ISO 8601 (기존 `SessionInfo.CreatedAt`의 `long` 타입 마이그레이션 필요)
- **MCP 직접 결합 금지**: draft가 안정될 때까지 내부 모델만 유지

### 참고 소스

- Kubernetes Jobs (TTL cleanup, finalizer, 상태 sub-object)
- tmux 세션 관리 (서버-클라이언트, 분리 생존)
- Docker 컨테이너 라이프사이클

---

## Phase 4A — Ghost Mode (Preflight Validation)

빌드/테스트 전 사전 검증. 범용 dry-run이 아닌 preflight validation.
기존 BuildHandler 코드를 재활용하므로 범위가 작고 빠름.

### 구현 방식

기존 `build` 커맨드에 `--dry-run` 파라미터 추가 (별도 커맨드 불필요).
Handler에서 `dryRun` 감지 시 검증만 수행하고 `BuildPipeline.BuildPlayer()` 호출하지 않음.

### 검증 항목 (3단계)

**Error (차단 — 빌드 확실히 실패)**:

| 검증 | Unity API |
|------|-----------|
| 빌드 타겟 문자열 유효성 | `ParseBuildTarget()` (기존 BuildHandler) |
| 플랫폼 모듈 설치 여부 | `BuildPipeline.IsBuildTargetSupported()` |
| 활성화된 씬 존재 여부 | `EditorBuildSettings.scenes` |
| 씬 파일 디스크 존재 | `File.Exists(scenePath)` |
| 스크립트 컴파일 에러 | `EditorUtility.scriptCompilationFailed` |
| 출력 경로 쓰기 가능 | `Directory.Exists()` / try-create |

**Warning (비차단 — 빌드 실패 가능 또는 예상치 못한 결과)**:

| 검증 | Unity API |
|------|-----------|
| 활성 빌드 타겟 불일치 (플랫폼 스위치 발생) | `EditorUserBuildSettings.activeBuildTarget` |
| Android SDK/NDK 경로 미발견 | `EditorPrefs.GetString("AndroidSdkRoot")` |
| iOS 타겟인데 macOS 아님 | `RuntimeInformation` |
| 현재 컴파일 중 (일시적) | `EditorApplication.isCompiling` |

**Info (참고)**:

| 검증 | 내용 |
|------|------|
| 활성화된 씬 목록 | 경로 나열 |
| 스크립팅 백엔드 | Mono vs IL2CPP |
| 스크립팅 define symbols | 현재 정의 |
| 기본 출력 경로 | 타겟별 |
| PlayerSettings 요약 | company, product, version |

### 검증 불가능 항목 (명시)

셰이더 컴파일, IL2CPP 코드 스트리핑, 에셋 임포트 (타겟별), 텍스처 압축 호환성,
빌드 크기 추정, 네이티브 플러그인 호환성, Gradle/Xcode 생성 오류.
이 항목들은 실제 빌드 없이는 검증 불가.

### 출력 형식

```json
{
  "statusCode": 0,
  "success": true,
  "message": "Preflight passed with 2 warnings",
  "data": {
    "dryRun": true,
    "target": "Android",
    "summary": { "errors": 0, "warnings": 2, "info": 5 },
    "checks": [
      { "category": "error", "check": "platform-module-installed", "passed": true, "message": "..." },
      { "category": "warning", "check": "platform-switch-required", "passed": false, "message": "..." }
    ]
  }
}
```

`success`는 error가 0이면 `true`. warning만으로는 `false`가 되지 않음 (terraform plan 모델).

### 참고 소스

- terraform plan (3-tier 출력, exit code 0/2)
- ansible --check, npm publish --dry-run, rsync --dry-run
- maxartz15/Validator, DarrenTsung/DTValidator (Unity 검증 프레임워크)
- Unity `BuildPlayerWindow.cs` 소스 (내부 검증 로직)

---

## Phase 3C — Watch Mode

Unity 이벤트를 Push 스트리밍으로 CLI에 실시간 전달.
IPC 프로토콜 확장이 필요하므로 다른 Phase보다 복잡.

### 아키텍처

**Push 모델 + 전용 영구 Named Pipe 연결.**

```text
CLI → Plugin:  subscribe 요청 {command: "watch", params: {channels: ["console","hierarchy","compilation"]}}
Plugin → CLI:  CommandResponse (구독 시작 확인)
Plugin → CLI:  EventEnvelope #1 (length-prefixed)
Plugin → CLI:  EventEnvelope #2 ...
Plugin → CLI:  ... (취소까지 지속)
```

기존 `[4-byte LE length][UTF-8 JSON]` 프레이밍 재사용.
별도 파이프명 고려: `unityctl_{hash}_watch` (커맨드 트래픽과 격리).

### 스레드 안전 전략

```text
[Unity 콜백] --enqueue--> [ConcurrentQueue<EventEnvelope>] --dequeue--> [Pipe Writer 스레드]
```

| 이벤트 소스 | 스레드 | 전략 |
|-------------|--------|------|
| `Application.logMessageReceivedThreaded` | **아무 스레드** (병렬!) | `ConcurrentQueue.Enqueue()`만. Unity API 호출 금지 |
| `EditorApplication.hierarchyChanged` | 메인 스레드 | Unity API 접근 안전. 결과를 enqueue |
| `CompilationPipeline.*` | 메인 스레드 | Unity API 접근 안전. 결과를 enqueue |

### 백프레셔 / 버퍼링

- **Bounded queue**: 1000 이벤트 상한
- **Drop 정책**: drop-oldest (Unity 콜백 차단 절대 금지)
- **Overflow 알림**: `eventType: "_overflow"` 합성 이벤트 + 드롭 수
- **하트비트**: 5~10초 간격 `_heartbeat` 이벤트 (stale 연결 감지)
- **종료**: `_close` 이벤트 전송 후 파이프 닫기 (의도적 종료 vs 크래시 구분)

### CLI UX

```
unityctl watch console              # 콘솔 로그만
unityctl watch all                   # 모든 채널
unityctl watch --format json         # NDJSON 출력 (jq 파이핑용)
unityctl watch --no-color            # 색상 비활성화
```

출력 형식 (text):
```
[14:32:01.123] [console/log]     Player connected
[14:32:01.456] [console/warning] Shader warning: ...
[14:32:02.001] [console/error]   NullReferenceException: ...
[14:32:10.234] [compilation]     Compilation started
[14:32:15.567] [compilation]     Compilation finished (success, 5.3s)
```

### 리스크 메모

| 리스크 | 완화 |
|--------|------|
| 도메인 리로드 → IPC 끊김 | CLI 자동 재연결 (지수 백오프). 갭 동안 이벤트 유실 감수 |
| 재귀 로깅 (`logMessageReceivedThreaded` 내 예외) | `[ThreadStatic] static bool _inHandler` 재진입 가드 |
| 메인 스레드 고갈 | `EditorApplication.update` 펌프에서 프레임당 최대 50개 처리 |
| 파이프 writer 차단 (CLI가 느리게 읽을 때) | Writer 스레드만 차단, 30초 미응답 시 클라이언트 연결 해제 |
| 다수 watcher 동시 연결 | `MaxServerInstances` 증가 또는 fan-out 패턴 |
| Plugin 동기 I/O 전용 | Unity Mono 비동기 파이프 미검증 → 전용 백그라운드 스레드에서 동기 write |

### 참고 소스

- `kubectl logs -f`, `docker logs --follow`, `tail -f`, `dotnet watch`
- 기존 `EventEnvelope` (`Shared/Protocol/`)
- 기존 `ITransport.SubscribeAsync` (`IAsyncEnumerable<EventEnvelope>`, Phase 3C stub)
- JetBrains Rider Unity 프로토콜 (영구 연결 스트리밍)

---

## Phase 4B — Scene Diff

Unity 씬 상태 스냅샷 + propertyPath 기반 diff.

### 기본 API

`SerializedObject` + `SerializedProperty` 순회 (YAML 파싱 아닌 런타임 API 우선).

- `new SerializedObject(obj)` → `GetIterator()` → `NextVisible(true)` 순회
- 각 `SerializedProperty`에서 `propertyPath`, `propertyType`, 값 추출
- `[SerializeReference]` 순환 참조 → `managedReferenceId` 추적으로 방지

### 오브젝트 식별

`GlobalObjectId` 사용 (에디터 세션 간 안정, 씬 내 유니크).
- 형식: `GlobalObjectId_V1-{identifierType}-{assetGUID}-{localFileID}-{prefabInstance}`
- **배치 API** `GetGlobalObjectIdsSlow(Object[], GlobalObjectId[])` 사용 (개별 호출보다 대폭 빠름)

### 스냅샷 스키마

```text
SceneSnapshot
├── timestamp: ISO 8601
├── unityVersion: string
├── sceneSetup[]: path, isLoaded, isActive  (멀티씬 설정)
├── scenes[]
│   ├── path, name, isDirty
│   └── gameObjects[]
│       ├── globalObjectId, name, activeSelf, layer, tag, scenePath
│       └── components[]
│           ├── globalObjectId, typeName, enabled
│           └── properties: Dictionary<propertyPath, {type, value}>
```

### Diff 알고리즘

```text
씬 매칭 (by path)
  → GameObject 매칭 (by GlobalObjectId) → ADDED / REMOVED / MODIFIED
    → Component 매칭 (by GlobalObjectId) → ADDED / REMOVED
      → Property Dict diff (by propertyPath) → ADDED / REMOVED / CHANGED
```

- **Float 비교**: epsilon 기반 (`1e-6` 기본, 설정 가능)
- **배열**: 인덱스 기반 positional diff (Unity 직렬화 방식과 일치)
- **Prefab override**: `PrefabUtility.GetPropertyModifications()` 활용하여 `isPrefabOverride` 플래그 가능

### 성능

- 5,000 GO 씬 (25,000 컴포넌트) → 2~10초 예상
- 최적화: `NextVisible()` (프로퍼티 30~50% 감소), 배치 GlobalObjectId, `scene.isDirty` 필터
- 대규모 씬 (100K+): 씬/경로 필터 제공, 진행 콜백

### YAML 파싱 (후순위)

Unity 커스텀 YAML (`!u!` 태그, `--- !u!<classID> &<fileID>`)은 표준 파서로 불가.
오프라인/CI용 미래 과제로 남김. Phase 4B에서는 Editor API만 사용.

### CLI 커맨드

```
unityctl scene snapshot --project <path> --json     # 스냅샷 캡처
unityctl scene diff <snap1> <snap2> --json          # 두 스냅샷 비교
unityctl scene diff --live --project <path>         # 현재 상태 vs 마지막 스냅샷
```

### 참고 소스

- Unity `SerializedObject`/`SerializedProperty` API
- Unity `GlobalObjectId` API
- Unity `PrefabUtility` override 감지
- Unity Smart Merge (UnityYAMLMerge, mergerules.txt)
- andrewmichaeljones/SceneDiff (텍스트 기반 씬 캡처)

---

## Phase 5 — Agent Layer

AI 에이전트가 unityctl을 도구로 활용하기 위한 외부 인터페이스.

### 핵심 원칙

- unityctl은 primitive 제공
- orchestration은 AI agent에 위임
- 네이티브 .NET MCP 서버로 직접 구현 (bridge 아님)

### 구현 우선순위

**P0 — Schema Command (필수, 가장 먼저)**:

```bash
unityctl schema --format json
```

모든 커맨드, 파라미터, 타입, 설명의 기계 판독 가능 스키마 출력.
에이전트와 MCP 브릿지가 도구 정의를 자동 생성하는 데 사용.

**P1 — MCP Server (`Unityctl.Mcp` 프로젝트)**:

```text
src/Unityctl.Mcp/  (net10.0)
  ├── NuGet: ModelContextProtocol (C# SDK v1.1.0)
  ├── NuGet: Unityctl.Core 참조
  ├── [McpServerTool] 래핑 (기존 커맨드별)
  └── Stdio transport (Claude Code, VS Code, Cursor 호환)
```

- 기존 커맨드를 `[McpServerTool]` + `[Description]` 어트리뷰트로 래핑
- DI로 Core 서비스 주입
- dotnet tool NuGet 패키지로 배포 가능
- 예상 규모: ~200줄 (얇은 래퍼)

**P2 — `unityctl exec` (C# 식 직접 실행)**:

```bash
unityctl exec "EditorApplication.isPlaying = true"
unityctl exec "AssetDatabase.Refresh()"
```

Unity Editor에 C# 식을 전송하여 IPC로 실행. 결과는 JSON 반환.
에이전트의 "escape hatch" — 기존 커맨드로 커버 안 되는 모든 Unity API 접근 가능.

**P3 — Workflow Runner (선택, 낮은 우선순위)**:

```bash
unityctl workflow run build-and-test.json
```

단순 순차 실행. `continueOnError`, `timeout` 정도만 지원.
복잡한 오케스트레이션은 에이전트 책임.

### 만들지 않을 것

- 커스텀 오케스트레이션 엔진 (에이전트가 오케스트레이터)
- 복잡한 워크플로 DSL (루프, 조건, 변수 — 에이전트 영역)
- 에이전트별 플러그인 (Claude 플러그인, GPT 플러그인 — MCP가 범용 통합점)
- 자연어 인터페이스 (에이전트가 NL 처리, unityctl은 typed tool)
- 상태 관리 (호출당 stateless, 상태는 에이전트 컨텍스트에)

### Agent-Friendly CLI 원칙

| 원칙 | unityctl 현황 |
|------|---------------|
| 구조화 JSON 출력 | ✅ `--json` 전체 지원 |
| 좁고 명확한 커맨드 | ✅ 커맨드별 단일 역할 |
| 결정적 exit code | ✅ 0 = 성공, 1 = 실패 |
| 멱등 동작 | ✅ 재실행 안전 |
| 기계 판독 에러 | ✅ JSON 에러 출력 |
| 대화형 프롬프트 없음 | ✅ stdin 미사용 |
| 스키마 자동 발견 | ✅ `unityctl schema --json` |
| 모든 작업에 timeout | ✅ `--timeout` 지원 |

### 경쟁 분석

| 프로젝트 | 특징 | unityctl 차별점 |
|----------|------|-----------------|
| UniCli | 80+ 커맨드, `eval`, Claude Code 플러그인 | batch mode + 에디터 디스커버리 + 크로스 플랫폼 |
| Unity MCP (CoplayDev) | MCP bridge, batch execute | 네이티브 .NET MCP (Python bridge 불필요) |
| UnityAgentClient | ACP (Zed) 기반 에디터 내 에이전트 | headless CI/CD 지원 + 외부 CLI |

### 참고 소스

- MCP Specification (2025-11-25)
- MCP C# SDK (modelcontextprotocol/csharp-sdk v1.1.0)
- Microsoft MCP .NET quickstart / samples
- 2026 MCP 로드맵 (async tasks, server discovery, scalable transport)
- UniCli, Unity MCP (CoplayDev), UnityAgentClient

---

## MCP 하이브리드 전략

상태: **구현 완료**

Write API (12개 write 명령)를 MCP에 노출할 때, 개별 tool로 펼치면 tools/list가 비대해져 토큰 우위가 반감됨.
해법: **`unityctl_run` 단일 tool로 수렴** + **`unityctl_schema(command=...)` 온디맨드 필터**.

### 설계

- 기존 12개 read/meta/streaming MCP 도구 유지
- write 계열은 `unityctl_run(project, command, parameters)` 하나로 접근
- `unityctl_run`은 allowlist 기반 — 35개 허용 명령 실행:
  Phase A/B/B.5: `play-mode`, `player-settings`, `asset-refresh`,
  `gameobject-create`, `gameobject-delete`, `gameobject-set-active`,
  `gameobject-move`, `gameobject-rename`, `scene-save`,
  `component-add`, `component-remove`, `component-set-property`
  Phase C: `asset-create`, `asset-create-folder`, `asset-copy`, `asset-move`, `asset-delete`, `asset-import`,
  `prefab-create`, `prefab-unpack`, `prefab-apply`, `prefab-edit`,
  `package-list`, `package-add`, `package-remove`, `project-settings-get`, `project-settings-set`,
  `material-get`, `material-set`, `material-set-shader`,
  `animation-create-clip`, `animation-create-controller`,
  `ui-canvas-create`, `ui-element-create`, `ui-set-rect`
- `unityctl_schema`에 `command` 파라미터 추가 — 단일 명령 스키마 온디맨드 조회
- MCP tool 수: 12 → **13** (tools/list 크기 ~500B 증가, 8.3x 우위 유지)
- Phase C 이후 allowlist: 12 → **39**개 (MCP tool 수 13개 유지)

### AI 에이전트 사용 흐름

```
1. AI: unityctl_schema(command="gameobject-create")
   → { "name": "gameobject-create", "parameters": [...] }

2. AI: unityctl_run(project="/path", command="gameobject-create", parameters='{"name":"Cube"}')
   → { "success": true, "globalObjectId": "...", "sceneDirty": true }

3. AI: unityctl_run(project="/path", command="scene-save")
   → { "success": true, "scenePath": "..." }
```

### 산출물

- `src/Unityctl.Mcp/Tools/RunTool.cs` — `unityctl_run` MCP 도구
- `src/Unityctl.Mcp/Tools/SchemaTool.cs` — `command` 파라미터 추가
- `tests/Unityctl.Mcp.Tests/` — RunTool/SchemaTool 필터 테스트 5개 추가 (총 16개)

---

## Write API Phase C — 커버리지 확장

상태: **구현 완료**

기존 Write API (12개 명령)에 23개 신규 명령 추가. 총 35개 write 명령.
MCP 도구 수 13개 유지 — 모든 신규 명령은 `unityctl_run` allowlist 추가만.

### 산출물 (Phase별)

| Sub-Phase | 명령 수 | 명령 |
|-----------|---------|------|
| C-1 Asset CRUD | 6 | asset-create, asset-create-folder, asset-copy, asset-move, asset-delete, asset-import |
| C-2 Prefab | 4 | prefab-create, prefab-unpack, prefab-apply, prefab-edit |
| C-3 Package/Settings | 5 | package-list, package-add, package-remove, project-settings-get, project-settings-set |
| C-4 Material/Shader | 3 | material-get, material-set, material-set-shader |
| C-5 Animation/UI | 5 | animation-create-clip, animation-create-controller, ui-canvas-create, ui-element-create, ui-set-rect |

### 파일 변경 범위

- Shared: `WellKnownCommands.cs` (+23 상수), `CommandCatalog.cs` (+23 Define)
- Plugin: 23개 핸들러 신규 (`Editor/Commands/`), `WellKnownCommands.cs` 동기화
- CLI: 7개 커맨드 파일 (AssetCommand 수정 + PrefabCommand/PackageCommand/ProjectSettingsCommand/MaterialCommand/AnimationCommand/UiCommand 신규), Program.cs (+23 등록)
- MCP: `RunTool.cs` allowlist 12→35개
- Tests: `CommandCatalogTests.cs` 이름 배열 갱신

### CoplayDev 대비 추정 대체율

> ⚠️ 아래 수치는 추정치. Write API 확장 27개 명령은 코드 구현 완료/빌드 통과이며, `scene open/create`, `undo/redo`는 Unity 실기 검증 완료. 나머지는 일부 미완.

| 완료 | AI 에이전트 일상 (추정) | CoplayDev 기능 패리티 (추정) |
|------|----------------------|---------------------------|
| Phase A/B/B.5 (12개) | ~75-85% | ~45-50% |
| + Phase C (35개) | **high-80s%** | **~60%** |

- "AI 에이전트 일상"은 GO/Component/Asset/Prefab/Material/Scene/Play/Test/Build 루프 기준.
- CoplayDev는 39개 도구에 manage_* sub-action 수백 개 (graphics 33, animation Cinemachine, vfx, probuilder, texture, terrain, audio 등).
- unityctl 독점 기능 (headless batch, dry-run, flight recorder, session, watch streaming, scene diff)은 CoplayDev에 없으므로 패리티 산정에 +25%p 가치.
- `exec` escape hatch를 고려하면 실질 커버리지는 수치 이상이나, 구조화 도구 대비 사용성 차이로 표에 미반영.
- 남은 핵심 공백: script editing, graphics/vfx, texture, multi-instance.

---

## 다음 우선순위

1. Phase 2B 후속 보강 (domain reload, batch IPC 미기동 로그, latency 측정)
2. Phase 1C 잔여 (release.yml, README)
3. Write API 확장 Unity 실측 검증 (asset/prefab/package/material/animation/ui)
