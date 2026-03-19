# README Benchmark for unityctl

최종 업데이트: 2026-03-18 (KST)

## 목적

`unityctl` README를 "기술 소개 문서"가 아니라 "GitHub star를 만드는 랜딩 페이지"로 재설계하기 위한 기준 문서입니다.

이 문서는 아래 두 가지를 동시에 다룹니다.

1. 유명하고 잘 만든 오픈소스 README들이 무엇을 잘하는지
2. 현재 `unityctl` README가 어디에서 강하고, 어디에서 전환이 끊기는지

## 벤치마크 기준 레포

기준은 "스타 수가 높고", "첫 화면에서 가치가 바로 보이며", "`unityctl`처럼 CLI/개발자 도구 맥락에 번역 가능한 README"를 우선으로 잡았습니다.

| Repo | Stars (2026-03-18) | 왜 참고할 만한가 | `unityctl`에 번역할 포인트 |
|------|--------------------|------------------|-----------------------------|
| [astral-sh/uv](https://github.com/astral-sh/uv) | 81.2k | 첫 화면에서 성능 수치와 설치 방법이 같이 보임 | 토큰 효율과 headless 강점을 hero 구간에서 바로 숫자로 제시 |
| [supabase/supabase](https://github.com/supabase/supabase) | 99.2k | 제품 한 줄 설명, 시각 자료, 문서/커뮤니티 동선이 매우 분명함 | README를 제품 랜딩처럼 구성하고 docs/community CTA를 선명하게 배치 |
| [sharkdp/bat](https://github.com/sharkdp/bat) | 57.7k | CLI 도구답게 예시 중심, 시각 데모 중심, 설치 동선이 빠름 | 실제 터미널 출력 예시와 GIF/스크린샷을 상단에 배치 |
| [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) | 7.2k | 같은 문제 영역에서 설치 옵션, 클라이언트 연결법, 최신 업데이트 노출이 강함 | 같은 Unity AI 도구 사용자 기준으로 설치/연결/지원 클라이언트 표를 강화 |

## 관찰 포인트

### 1. `uv` 패턴

- Hero 아래에서 바로 "왜 써야 하는가"가 bullet로 정리됩니다.
- 성능 수치가 README 상단에 이미지와 문장으로 같이 보입니다.
- 설치가 OS별 copy-paste 명령으로 즉시 이어집니다.
- 상세 설명은 docs로 보내고, README는 전환 중심으로 유지합니다.

`unityctl` 번역:

- 첫 화면에서 "MCP 호환 Unity 제어 + headless CI/CD + 토큰 효율"을 한 문장으로 못 박아야 합니다.
- `schema`/MCP/tools-list 토큰 비교와 `build --dry-run` 데모를 상단으로 올려야 합니다.

### 2. `Supabase` 패턴

- 제품 정의가 한 줄로 명확합니다.
- 스크린샷이 있어 README가 문서보다 제품처럼 보입니다.
- 문서, 커뮤니티, 기여 경로가 초반부터 명확합니다.
- "어떻게 동작하는가"는 후반부로 보내고, 초반은 가치 전달에 집중합니다.

`unityctl` 번역:

- 아키텍처보다 먼저 "누가 왜 써야 하는가"를 보여줘야 합니다.
- AI agent용, CI용, Unity tooling용 사용 시나리오를 초반에 구분해야 합니다.
- docs 링크, release 링크, MCP client 예시 링크를 눈에 띄게 배치해야 합니다.

### 3. `bat` 패턴

- 설명보다 먼저 결과 화면이 나옵니다.
- CLI README답게 예제가 짧고 복붙 가능하며, 실사용 장면이 곧바로 보입니다.
- 설치 섹션이 풍부해서 "내 환경에서 바로 가능할까?"에 대한 불안을 줄입니다.

`unityctl` 번역:

- `watch`, `scene diff`, `exec`, `build --dry-run` 결과 화면을 GIF 또는 캡처로 보여줘야 합니다.
- "3 commands to first success"를 더 짧게 만들어야 합니다.
- `dotnet run`만이 아니라 release binary, `dotnet tool`, MCP server 실행법을 같이 보여줘야 합니다.

### 4. `CoplayDev/unity-mcp` 패턴

- 같은 Unity AI 도구 시장에서 "지원 클라이언트", "설치 옵션", "최근 업데이트"를 적극적으로 전면 배치합니다.
- 설치가 Unity Package Manager, Asset Store, OpenUPM처럼 여러 경로로 제공됩니다.
- README만 보고도 설정이 가능할 정도로 사용자 여정이 촘촘합니다.

`unityctl` 번역:

- `unityctl`의 차별점은 도구 수가 아니라 "headless + schema + native .NET MCP + 토큰 효율"입니다.
- 그러므로 README는 기능 나열보다 "왜 CoplayDev와 다른가"를 숫자와 시나리오로 설득해야 합니다.

## README 벤치마크 채점표

총점 100점. 70점 이상이면 공개 확산용으로 경쟁력이 있고, 85점 이상이면 star 전환 효율이 높은 README로 봅니다.

| 항목 | 배점 | 기준 |
|------|------|------|
| Hero 메시지 | 15 | 첫 화면 10초 안에 무엇이고 왜 중요한지 이해되는가 |
| 설치 마찰 | 15 | 최소 경로 설치가 3분 안에 가능한가 |
| 차별점 증명 | 20 | 숫자, 벤치마크, 비교 표로 강점이 증명되는가 |
| Copy-paste 예제 | 10 | 예제가 짧고 바로 실행 가능한가 |
| 시각 데모 | 10 | GIF, 터미널 캡처, 스크린샷이 있는가 |
| 신뢰 신호 | 10 | 테스트, CI, 릴리스, 플랫폼 지원이 명확한가 |
| 통합성 | 10 | Claude/Cursor/VS Code/CI 등 실제 연결 경로가 보이는가 |
| 정보 구조 | 5 | README 흐름이 hero → quickstart → proof → details 순으로 잘 정리되는가 |
| 커뮤니티 동선 | 5 | docs, issues, discussions, contributing 경로가 선명한가 |

## 현재 unityctl 1차 점수

기준 파일: [README.md](../../README.md)

| 항목 | 점수 | 메모 |
|------|------|------|
| Hero 메시지 | 9/15 | 방향은 좋지만 가장 강한 메시지인 토큰 효율과 headless 우위가 첫 화면에서 약함 |
| 설치 마찰 | 5/15 | `dotnet build` 중심이고, clone 예시도 현재 잘못되어 있음 |
| 차별점 증명 | 10/20 | 비교 표는 있지만 실제 측정 링크, 벤치마크 산출물, 시각 증거가 없음 |
| Copy-paste 예제 | 8/10 | 커맨드 예제는 충분함 |
| 시각 데모 | 0/10 | GIF/스크린샷/터미널 캡처가 없음 |
| 신뢰 신호 | 7/10 | 테스트 수, 플랫폼 표는 좋음 |
| 통합성 | 6/10 | MCP server 존재는 보이지만 클라이언트별 설정 예시는 약함 |
| 정보 구조 | 4/5 | 큰 흐름은 나쁘지 않음 |
| 커뮤니티 동선 | 1/5 | docs, issue, contribution 동선이 약함 |
| 합계 | 50/100 | 기술 소개 README로는 괜찮지만 star 전환형 README로는 부족 |

## 현재 README의 주요 병목

### P0

1. clone URL 오류 수정
   - 현재 README는 `unityctl`이 아니라 `unityagent`를 clone 하도록 적혀 있습니다.
2. 상단 차별점 재배치
   - "토큰 효율 9x", "Editor 없이 check/test/build", "native .NET MCP"를 첫 화면에 올려야 합니다.
3. 벤치마크 산출물 부재
   - README에 숫자가 있어도 재현 가능한 `docs/benchmark/` 결과물이 없으면 신뢰가 약합니다.
4. 시각 증거 부재
   - `build --dry-run`, `watch`, `scene diff`, `exec` 중 2개 이상은 화면으로 보여줘야 합니다.
5. 설치 경로 부족
   - release binary, `dotnet tool`, MCP client 연결 예시가 부족합니다.

### P1

1. "누구를 위한 도구인가" 구간 추가
   - AI coding agent 사용자
   - Unity CI/CD 사용자
   - Unity Editor 자동화 사용자
2. 지원 클라이언트/환경 표 추가
   - Claude Code, Cursor, VS Code, GitHub Actions
3. "왜 기존 Unity MCP가 아니라 unityctl인가" 구간 추가
   - 기능 수가 아니라 headless, fallback, schema, token cost를 중심으로 비교
4. docs 동선 강화
   - Getting Started, AI Quickstart, Architecture, Benchmark 링크를 초반에 배치

## 추천 README 정보 구조

아래 순서가 `unityctl`에 가장 맞습니다.

1. Hero
   - 한 줄 정의
   - 핵심 차별점 3개
   - 짧은 설치/실행 3줄
2. Why unityctl
   - 토큰 효율
   - headless CI/CD
   - IPC to batch fallback
3. Proof
   - 벤치마크 표
   - 짧은 GIF 2개
4. Quick Start
   - CLI
   - MCP server
   - Unity plugin
5. Real Workflows
   - `check`
   - `build --dry-run`
   - `test`
   - `watch`
   - `exec`
6. Integrations
   - Claude Code
   - Cursor
   - VS Code
   - GitHub Actions
7. Trust
   - 테스트 수
   - 플랫폼
   - 릴리스
8. Docs / Contributing / License

## README 개편 시 반드시 넣을 문장 후보

### Hero 초안

> `unityctl` is a headless-first Unity control plane for AI agents and CI pipelines, with native .NET MCP support, IPC-to-batch fallback, and lower tool-context cost than generic Unity MCP setups.

### 서브 포인트 초안

- Run `check`, `test`, and `build --dry-run` without a live Unity Editor
- Export a machine-readable schema for agents with `unityctl schema --format json`
- Use resident MCP mode when an Editor is open, and batch fallback when it is not

## 다음 작업 우선순위

1. `docs/benchmark/benchmark-results.md`와 `token-comparison.md` 실측값으로 채우기
2. README hero를 토큰 효율 중심으로 재작성
3. GIF 또는 터미널 캡처 2개 제작
4. `dotnet tool` 또는 release binary 설치 섹션 추가
5. MCP client 설정 예시 추가

## 참고 링크

- [astral-sh/uv](https://github.com/astral-sh/uv)
- [supabase/supabase](https://github.com/supabase/supabase)
- [sharkdp/bat](https://github.com/sharkdp/bat)
- [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp)
