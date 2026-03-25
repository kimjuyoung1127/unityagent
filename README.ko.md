# unityctl

[English](README.md) | [한국어](README.ko.md)

[![NuGet](https://img.shields.io/nuget/v/unityctl?label=unityctl)](https://www.nuget.org/packages/unityctl)
[![NuGet](https://img.shields.io/nuget/v/unityctl-mcp?label=unityctl-mcp)](https://www.nuget.org/packages/unityctl-mcp)
[![CI](https://github.com/kimjuyoung1127/unityctl/actions/workflows/ci-dotnet.yml/badge.svg)](https://github.com/kimjuyoung1127/unityctl/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

### AI가 게임을 만들 수 있게 해주는 실행 레이어.

AI 에이전트에 **133개 명령**을 쥐여주세요. Unity 씬 구성부터 C# 스크립트 작성, 빌드 검증, 게임 배포까지 — 문제가 생기면 자동으로 롤백됩니다.

```
133 CLI 명령 · 12 MCP 도구 · 689 테스트 · Windows / macOS / Linux
```

<p align="center">
  <img src="docs/assets/mcp-demo.svg" alt="AI 에이전트가 MCP를 통해 Unity 씬을 구성하는 모습" width="700">
</p>

---

## 문제

AI 에이전트는 코드는 잘 쓰는데, **게임은 못 만듭니다.** Unity에 씬 편집이나 에셋 관리, 프로젝트 검증을 위한 프로그래밍 인터페이스가 없기 때문입니다.

기존 Unity MCP 서버들이 이 문제를 해결하려 했지만, 오히려 새로운 문제를 만들었습니다:

| 문제점 | AI 에이전트에 미치는 영향 |
|---|---|
| 매 턴 **45 KB 이상의 스키마** 로드 | 추론 대신 도구 정의에 토큰을 낭비 |
| **검증 피드백이 없음** | 변경 후 씬이 깨졌는지 알 방법이 없음 |
| **롤백이 불가능** | 명령 하나 잘못하면 프로젝트 상태가 오염됨 |
| **Play Mode에서 WebSocket이 끊김** | Unity Domain Reload 과정에서 연결이 끊어짐 |
| **에디터가 반드시 열려 있어야 함** | GUI 없는 CI/CD 환경에서 사용 불가 |

## 해결

unityctl은 Unity Editor를 프로그래밍 가능한 API로 만드는 **.NET CLI + MCP 서버**입니다.

에이전트가 명령을 _실행_하는 것에 그치지 않고, 결과를 _검증_하고, 실패를 _진단_하고, 실수를 _복구_할 수 있는 **폐루프 자동화**를 제공합니다:

<p align="center">
  <img src="docs/assets/agent-loop.svg" alt="계획 - 실행 - 검증 - 진단 루프" width="680">
</p>

> **다른 도구는 에이전트에게 손만 줍니다. unityctl은 손, 눈, 그리고 안전망까지 줍니다.**

---

## AI 에이전트로 만들 수 있는 것

### 씬 구성

> _"바닥, 벽, 플레이어 스폰 포인트가 있는 플랫포머 레벨 만들어줘"_

```bash
# 씬 구조 생성
unityctl scene create --path "Assets/Scenes/Level01.unity" --project $P
unityctl mesh create-primitive --type Plane --name "Floor" --scale "[10,1,10]" --project $P
unityctl mesh create-primitive --type Cube --name "Wall" --position "[5,1,0]" --scale "[0.5,2,10]" --project $P
unityctl gameobject create --name "PlayerSpawn" --project $P
unityctl component add --id "<PlayerSpawnId>" --type "BoxCollider" --project $P

# 씬 검증
unityctl scene hierarchy --project $P --json      # 구조 확인
unityctl screenshot capture --project $P           # 시각 검증
unityctl project validate --project $P --json      # 카메라? 조명? 에러?
```

### 스크립트 작성 + 컴파일 검증

> _"플레이어 이동 스크립트 작성하고 컴파일 되는지 확인해줘"_

```bash
# 코드 작성
unityctl script create --path "Assets/Scripts/PlayerMovement.cs" --className "PlayerMovement" --project $P
unityctl script patch --path "Assets/Scripts/PlayerMovement.cs" \
  --startLine 8 --insertContent "public float speed = 5f;" --project $P

# 컴파일 확인 → 에러 발생 시 루프로 수정
unityctl script validate --project $P --wait       # 리컴파일 트리거
unityctl script get-errors --project $P --json     # 구조화된 CS 에러
# 에러가 있으면: 에러 확인 → 패치 수정 → 재검증
```

### 안전한 배치 작업 (롤백 포함)

> _"Player, Enemy, Projectile 물리 레이어 설정해줘 — 실패하면 되돌려"_

```bash
unityctl batch execute --project $P --rollbackOnFailure true --commands '[
  {"command": "layer-set", "parameters": {"index": 8, "name": "Player"}},
  {"command": "layer-set", "parameters": {"index": 9, "name": "Enemy"}},
  {"command": "layer-set", "parameters": {"index": 10, "name": "Projectile"}},
  {"command": "physics-set-collision-matrix", "parameters": {"layer1": 10, "layer2": 10, "ignore": true}}
]'
# 하나라도 실패하면 모든 변경사항이 Undo로 자동 롤백
```

### 빌드 검증 파이프라인

> _"프로젝트 출시 준비 됐는지 확인해줘"_

<p align="center">
  <img src="docs/assets/project-validate.svg" alt="6개 항목을 검사하는 project-validate 출력" width="600">
</p>

```bash
# 실패 원인을 읽고 수정한 뒤 재검증
unityctl gameobject create --name "Main Camera" --project $P
unityctl component add --id "<MainCameraId>" --type "Camera" --project $P
unityctl gameobject set-tag --id "<MainCameraId>" --tag "MainCamera" --project $P
unityctl project validate --project $P --json   # valid: true
```

---

## 첫 번째 쇼케이스 추천

unityctl을 공개적으로 보여주고 싶다면, 마인크래프트부터 시작하지 마세요.
현재 툴체인에서 가장 잘 검증된 루프에 맞는 순서대로 올라가세요:

1. **Zero-to-playable**: 프리미티브, 스크립트, UI, 물리, 검증 결과물로 만드는 작은 3D 아레나 마이크로게임.
2. **버티컬 슬라이스**: 프리팹, NavMesh, 머티리얼, 오디오, 빌드 검증까지 포함된 탑다운 서바이벌 또는 기지 방어 프로토타입.
3. **샌드박스 단계**: 그 다음에 청크 월드, 크래프팅, 절차적 터레인, 세이브 시스템 등으로 확장.

첫 쇼케이스로 가장 좋은 건 오픈월드 샌드박스가 아니라 **작은 3D 서바이벌/기지 방어 게임**입니다.
스크린샷과 GIF로 바로 이해할 수 있고, 씬 편집 + 스크립트 패칭 + 롤백 + 시각 검증 루프에 딱 맞으며, 이후에 더 복잡한 샌드박스로 확장할 수 있습니다.

자세한 내용은 [쇼케이스 로드맵](docs/ref/showcase-roadmap.md)을 참고하세요.

---

## 왜 unityctl인가?

| | unityctl | 기존 Unity MCP |
|---|---|---|
| **스키마 오버헤드** | 세션당 **5 KB** (9배 작음) | 매 턴 45 KB 이상 |
| **검증 루프** | `project validate` + `scene diff` + `screenshot capture` | 에이전트가 결과를 확인할 수 없음 |
| **에러 복구** | `script get-errors`로 파일/줄/열/에러코드까지 구조화 | 콘솔 로그 원문 그대로이거나 아예 없음 |
| **안전한 실험** | `batch execute --rollbackOnFailure` + `undo` | 롤백 없음 — 실수가 그대로 남음 |
| **연결 안정성** | Named Pipe — Domain Reload에서도 끊기지 않음 | WebSocket 끊김, 수동 재연결 필요 |
| **CI/CD** | `check` / `test` / `build --dry-run` 헤드리스 지원 | 에디터를 열어야만 동작 |
| **진단** | `doctor`가 실패를 분류하고 다음 조치를 안내 | "Connection failed"만 출력 |
| **명령 수** | **133** (읽기 + 쓰기 + 검증 + 진단) | ~34-200 |
| **감사 추적** | 모든 명령의 NDJSON 플라이트 레코더 | 이력 없음 |
| **런타임** | 네이티브 .NET — Python/TS 브릿지 불필요 | 브릿지 오버헤드 있음 |
| **설치** | `dotnet tool install -g unityctl` | Node.js + npm + 포트 설정 |
| **라이선스** | **MIT** | 다양 |

### 토큰 효율성

AI 에이전트 비용의 대부분은 매 턴 전송되는 도구 스키마에서 발생합니다. unityctl은 **온디맨드 스키마 로딩**으로 이 비용을 극적으로 줄입니다:

<p align="center">
  <img src="docs/assets/token-efficiency.svg" alt="실측 토큰 비용: unityctl via Bash = 오버헤드 0, CoplayDev MCP 대비 6.8배 저렴" width="620">
</p>

12개 MCP 도구가 `unityctl_query`(읽기), `unityctl_run`(쓰기), `unityctl_schema`(조회)를 통해 133개 명령 전체를 커버합니다.

#### 실측: Claude Code 토큰 비용 (2026-03-20)

Claude Code에서 5가지 읽기 전용 QA 작업(컴파일 체크, 씬 계층, 로봇 카탈로그, DH 테이블, 빌드 설정)을 실행했을 때의 **누적 토큰 비용**:

| 스택 | 스키마 (1회) | 5 ops x 1 | 5 ops x 10 |
|---|---:|---:|---:|
| **unityctl via Bash** | **0 tok** | **1,780 tok** | **17,800 tok** |
| unityctl MCP (12 도구) | 1,256 tok | 2,957 tok | 18,261 tok |
| CoplayDev MCP (30 도구) | 11,427 tok | 12,158 tok | 18,742 tok |

핵심:
- **unityctl via Bash는 스키마 오버헤드가 0** — Bash 도구가 이미 Claude Code 시스템 프롬프트에 포함되어 있어 도구 정의에 추가 토큰이 들지 않음
- CoplayDev MCP는 **45 KB**(30개 도구) 스키마를 로드하지만, 5개 QA 작업 중 실제로 대응할 수 있는 건 **1개뿐**
- 일반적인 짧은 세션에서 unityctl via Bash가 CoplayDev MCP 대비 **6.8배 적은 토큰**을 사용
- 전체 벤치마크 방법론과 원시 데이터: [`docs/benchmark/`](docs/benchmark/)

---

## 선택 기반 라우팅

```bash
# 사용할 Unity 프로젝트를 한 번 지정해두면
unityctl editor select --project /path/to/project

# 또는 실행 중인 Unity PID로 지정
unityctl editor select --pid 55028

# 현재 어디를 가리키고 있는지 확인
unityctl editor current --json

# 실행 중인 Unity 인스턴스 목록 (PID / 프로젝트 / IPC 상태)
unityctl editor instances --json

# 이후부터는 --project 생략 가능
unityctl ping --json
unityctl status --json
unityctl check --json
unityctl doctor --json

# 검증 번들 실행 (아티팩트 우선)
unityctl workflow verify --file verify.json --project /path/to/project --json
```

## 설치

```bash
# CLI (.NET 10 SDK 필요)
dotnet tool install -g unityctl

# AI 에이전트용 MCP 서버
dotnet tool install -g unityctl-mcp
```

참고:
- `--source`에 로컬 `Unityctl.Plugin` 폴더 경로나 Git URL을 넣을 수 있습니다: `https://github.com/kimjuyoung1127/unityctl.git?path=/src/Unityctl.Plugin#v0.3.2`
- GitHub Release의 CLI 아카이브는 현재 framework-dependent 빌드입니다 (self-contained 아님).

### Apple Silicon macOS 검증

Apple Silicon MacBook Air에서 Homebrew, .NET SDK `10.0.105`, Unity Hub, Unity `6000.0.64f1` / `6000.3.11f1`로 검증 완료.

검증 경로:

- `dotnet tool install -g unityctl`
- `dotnet tool install -g unityctl-mcp`
- `unityctl editor list`
- `unityctl init --project <project> --source /path/to/unityctl/src/Unityctl.Plugin`
- `unityctl ping --project <project> --json`
- `unityctl doctor --project <project> --json`
- `unityctl status --project <project> --json`
- `unityctl check --project <project> --json`

결과: `ping` → pong 반환, `doctor` → IPC 연결 확인, `status` → Ready, `check` → macOS 통과.

프로젝트 호환성 주의: 프로젝트나 서드파티 패키지가 Unity `6.0 LTS`에 고정되어 있으면, `6000.3+`에서 열었을 때 unityctl과 무관하게 에러가 발생할 수 있습니다. 고정된 `6000.0.64f1`에서 다시 열면 해결됩니다.

## 빠른 시작

```bash
# 1. 에디터 플러그인 설치
unityctl init --project /path/to/project \
  --source "https://github.com/kimjuyoung1127/unityctl.git?path=/src/Unityctl.Plugin#v0.3.2"

# 2. Unity Editor에서 프로젝트를 열고 연결 확인
unityctl ping --project /path/to/project --json
unityctl status --project /path/to/project --json

# 3. 만들기 시작
unityctl gameobject create --name "Player" --project /path/to/project
unityctl component add --id "<PlayerId>" --type "Rigidbody" --project /path/to/project
unityctl scene save --project /path/to/project

# 4. 검증
unityctl project validate --project /path/to/project --json

# 5. 빌드
unityctl build --project /path/to/project --dry-run    # 13개 사전 검증
```

### MCP 설정 (AI 에이전트)

Claude Code / Cursor / VS Code MCP 설정에 추가:

```json
{
  "mcpServers": {
    "unityctl": {
      "command": "unityctl-mcp"
    }
  }
}
```

<details>
<summary><strong>12 MCP 도구</strong></summary>

| 도구 | 타입 | 설명 |
|------|------|------|
| `unityctl_query` | 읽기 | 에셋, 게임오브젝트, 씬, 컴포넌트, UI, 물리, 조명, 태그 통합 조회 |
| `unityctl_run` | 쓰기 | 생성, 삭제, 수정, 스크립트, 머티리얼, 프리팹, 배치 통합 실행 |
| `unityctl_schema` | 메타 | 명령별 또는 카테고리별 파라미터 온디맨드 조회 |
| `unityctl_build` | 액션 | 13개 사전 검증 포함 플레이어 빌드 |
| `unityctl_check` | 액션 | 컴파일 검증 (헤드리스 지원) |
| `unityctl_test` | 액션 | EditMode / PlayMode 테스트 |
| `unityctl_exec` | 액션 | 임의의 C# 표현식 실행 |
| `unityctl_status` | 읽기 | 에디터 상태 + 연결 정보 |
| `unityctl_ping` | 읽기 | 연결 확인 |
| `unityctl_watch` | 스트림 | 실시간 콘솔/계층/컴파일 이벤트 |
| `unityctl_log` | 읽기 | 플라이트 레코더 조회 |
| `unityctl_session_list` | 읽기 | 활성 세션 목록 |

</details>

---

## 명령어 (133)

### 코어 (13)

| 명령어 | 설명 |
|--------|------|
| `ping` | Unity 연결 확인 |
| `status` | 에디터 상태 확인 (Domain Reload 대응 `--wait` 폴링) |
| `check` | 스크립트 컴파일 검증 (헤드리스) |
| `build` | 플레이어 빌드 (`--dry-run`으로 13개 사전 검증) |
| `test` | EditMode / PlayMode 테스트 |
| `doctor` | 연결 진단 + 복구 조치 안내 |
| `project validate` | 게임 준비 상태 점검 (컴파일, 씬, 카메라, 조명, 콘솔, 에디터) |
| `init` | Unity 프로젝트에 플러그인 설치 |
| `editor list` | 설치된 Unity 에디터 탐색 |
| `editor instances` | 실행 중인 Unity Editor 인스턴스 목록 |
| `editor current` | 현재 선택된 프로젝트 타겟 확인 |
| `editor select` | CLI 라우팅 대상 프로젝트 또는 PID 지정 |
| `workflow verify` | 아티팩트 우선 검증 (`projectValidate`, `capture`, `imageDiff`, `consoleWatch`, `uiAssert`, `playSmoke`) |

<details>
<summary><strong>씬 & 게임오브젝트</strong> (19)</summary>

| 명령어 | 설명 |
|--------|------|
| `scene snapshot` | 씬 상태 캡처 |
| `scene hierarchy` | 씬 계층 트리 |
| `scene diff` | 프로퍼티 수준 씬 비교 (epsilon 지원) |
| `scene save` | 활성 씬 저장 |
| `scene open` | 경로로 씬 열기 |
| `scene create` | 새 씬 생성 |
| `gameobject create` | 게임오브젝트 생성 |
| `gameobject delete` | 게임오브젝트 삭제 |
| `gameobject rename` | 게임오브젝트 이름 변경 |
| `gameobject move` | 게임오브젝트 부모 변경 |
| `gameobject find` | 이름/태그/컴포넌트로 검색 |
| `gameobject get` | 게임오브젝트 상세 정보 |
| `gameobject set-active` | 활성 상태 토글 |
| `gameobject set-tag` | 태그 설정 |
| `gameobject set-layer` | 레이어 설정 |
| `component add` | 컴포넌트 추가 |
| `component remove` | 컴포넌트 제거 |
| `component get` | 컴포넌트 프로퍼티 조회 |
| `component set-property` | 컴포넌트 프로퍼티 설정 |

</details>

<details>
<summary><strong>에셋 & 머티리얼</strong> (21)</summary>

| 명령어 | 설명 |
|--------|------|
| `asset find` | 타입/라벨/경로로 검색 |
| `asset get-info` | 에셋 메타데이터 |
| `asset get-dependencies` | 직접 의존성 |
| `asset reference-graph` | 역참조 그래프 |
| `asset create` | 에셋 생성 |
| `asset create-folder` | 폴더 생성 |
| `asset copy` | 에셋 복사 |
| `asset move` | 에셋 이동/이름 변경 |
| `asset delete` | 에셋 삭제 |
| `asset import` | 에셋 리임포트 |
| `asset refresh` | AssetDatabase 새로고침 |
| `asset get-labels` | 라벨 조회 |
| `asset set-labels` | 라벨 설정 |
| `material create` | 머티리얼 생성 |
| `material get` | 머티리얼 프로퍼티 조회 |
| `material set` | 머티리얼 프로퍼티 설정 |
| `material set-shader` | 셰이더 변경 |
| `prefab create` | 게임오브젝트에서 프리팹 생성 |
| `prefab unpack` | 프리팹 인스턴스 언팩 |
| `prefab apply` | 프리팹 오버라이드 적용 |
| `prefab edit` | 프리팹 편집 모드 진입/종료 |

</details>

<details>
<summary><strong>스크립팅 & 코드 분석</strong> (10)</summary>

| 명령어 | 설명 |
|--------|------|
| `script create` | 템플릿 기반 C# 스크립트 생성 |
| `script edit` | 스크립트 전체 교체 |
| `script patch` | 줄 단위 삽입/삭제/교체 |
| `script delete` | 스크립트 파일 삭제 |
| `script validate` | 컴파일 트리거 및 검증 |
| `script list` | MonoScript 에셋 목록 |
| `script get-errors` | 구조화된 컴파일 에러 (파일/줄/열/코드) |
| `script find-refs` | 전체 스크립트에서 심볼 참조 검색 |
| `script rename-symbol` | 전체 스크립트에서 심볼 이름 변경 (`--dry-run` 지원) |
| `exec` | Unity에서 C# 표현식 실행 |

</details>

<details>
<summary><strong>에디터 제어</strong> (18)</summary>

| 명령어 | 설명 |
|--------|------|
| `play start/stop/pause` | 플레이 모드 시작/중지/일시정지 |
| `editor pause` | 에디터 일시정지 토글 |
| `editor focus-gameview` | Game View 포커스 |
| `editor focus-sceneview` | Scene View 포커스 |
| `player-settings get/set` | PlayerSettings 읽기/쓰기 |
| `project-settings get/set` | 프로젝트 설정 읽기/쓰기 |
| `console clear` | 콘솔 초기화 |
| `console get-count` | 로그/경고/에러 카운트 |
| `define-symbols get/set` | 스크립팅 정의 심볼 |
| `tag list/add` | 태그 관리 |
| `layer list/set` | 레이어 관리 |
| `undo` | 마지막 작업 취소 |
| `redo` | 마지막 취소를 되돌리기 |

</details>

<details>
<summary><strong>빌드 & 배포</strong> (6)</summary>

| 명령어 | 설명 |
|--------|------|
| `build-profile list/get-active/set-active` | 빌드 프로필 관리 |
| `build-target switch` | 빌드 플랫폼 전환 |
| `build-settings get-scenes/set-scenes` | 빌드 씬 목록 |

</details>

<details>
<summary><strong>물리, 조명 & NavMesh</strong> (12)</summary>

| 명령어 | 설명 |
|--------|------|
| `physics get-settings/set-settings` | DynamicsManager |
| `physics get-collision-matrix/set-collision-matrix` | 32x32 레이어 충돌 |
| `lighting bake/cancel/clear` | 라이트맵 베이킹 |
| `lighting get-settings/set-settings` | 라이트맵 설정 |
| `navmesh bake/clear/get-settings` | NavMesh |

</details>

<details>
<summary><strong>UI & 메시</strong> (8)</summary>

| 명령어 | 설명 |
|--------|------|
| `ui canvas-create` | UI Canvas 생성 |
| `ui element-create` | Button, Text, Image 등 생성 |
| `ui set-rect` | RectTransform 설정 |
| `ui find` | UI 요소 검색 |
| `ui get` | UI 요소 상세 정보 |
| `ui toggle` | Toggle 상태 설정 |
| `ui input` | InputField 텍스트 설정 |
| `mesh create-primitive` | Cube/Sphere/Plane/Cylinder/Capsule/Quad 생성 |

</details>

<details>
<summary><strong>자동화 & 모니터링</strong> (15)</summary>

| 명령어 | 설명 |
|--------|------|
| `batch execute` | 롤백 포함 트랜잭션 실행 |
| `workflow run` | JSON 워크플로우 실행 |
| `watch` | 실시간 이벤트 스트리밍 |
| `log` | 플라이트 레코더 조회 |
| `session list/stop/clean` | 세션 관리 |
| `screenshot` | Scene/Game View 캡처 (base64) |
| `schema` / `tools` | 머신 리더블 메타데이터 |
| `package list/add/remove` | 패키지 관리 |
| `animation create-clip/create-controller` | 애니메이션 에셋 |

</details>

---

## 아키텍처

```
AI 에이전트 (LLM)            unityctl-mcp              unityctl CLI             Unity Editor
Claude / GPT / Gemini         12 MCP 도구               131 명령                 플러그인 (IPC)
        |                          |                          |                       |
        |--- MCP (stdio) -------->|                          |                       |
        |                          |--- CLI 호출 ----------->|                       |
        |                          |                          |--- IPC (~100ms) ---->|
        |                          |                          |    또는 Batch (30s+)  |
        |                          |                          |<--- JSON 응답 -------|
        |                          |<--- 결과 ---------------|                       |
        |<--- 도구 결과 ----------|                          |                       |
```

```
unityctl.slnx
+-- src/Unityctl.Shared   (netstandard2.1)  프로토콜 + 모델
+-- src/Unityctl.Core     (net10.0)         비즈니스 로직
+-- src/Unityctl.Cli      (net10.0)         CLI 셸
+-- src/Unityctl.Mcp      (net10.0)         MCP 서버
+-- src/Unityctl.Plugin   (Unity UPM)       에디터 브릿지 (IPC 서버)
+-- tests/*                                 633 xUnit 테스트
```

---

## 플랫폼

| 플랫폼 | CLI | IPC 전송 | Batch | CI |
|--------|-----|----------|-------|----|
| Windows | ✅ | Named Pipe | ✅ | ✅ |
| macOS | ✅ | Unix Domain Socket | ✅ | ✅ |
| Linux | ✅ | Unix Domain Socket | ✅ | ✅ |

## 요구사항

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Unity 2021.3+](https://unity.com/download)

## 터미널 출력

<p align="center">
  <img src="docs/assets/editor-list.svg" alt="unityctl editor list" width="570">
</p>

<p align="center">
  <img src="docs/assets/log-table.svg" alt="unityctl log" width="645">
</p>

<p align="center">
  <img src="docs/assets/tools.svg" alt="unityctl tools — 9개 카테고리 133개 명령" width="654">
</p>

## 문서

- [시작하기](docs/ref/getting-started.md) — 설치, 설정, 주요 워크플로우
- [AI 에이전트 빠른 시작](docs/ref/ai-quickstart.md) — MCP 설정 및 에이전트 연동 가이드
- [쇼케이스 로드맵](docs/ref/showcase-roadmap.md) — 추천 데모 구성, 에셋 체크리스트, 사전 작업 계획
- [아키텍처](docs/ref/architecture-mermaid.md) — 시스템 설계 및 전송 계층 다이어그램
- [용어 사전](docs/ref/glossary.md) — 주요 용어와 개념

## 변경 이력

버전 이력은 [GitHub Releases](https://github.com/kimjuyoung1127/unityctl/releases)를 확인하세요.

## 라이선스

MIT — [LICENSE](LICENSE) 참고
