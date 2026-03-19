# Task: unityctl vs Unity MCP 성능 비교 벤치마크

## 목표
unityctl과 기존 Unity MCP(CoplayDev) 솔루션의 성능을 정량적으로 비교하는 벤치마크를 설계하고 실행.

## 시작 전 필독
1. `AGENTS.md` — 프로젝트 구조 파악
2. `docs/ref/ai-quickstart.md` — CLI 사용법
3. `docs/status/PROJECT-STATUS.md` — 현재 상태

## 비교 대상

### A. unityctl (이 프로젝트)
- 실행: `dotnet run --project src/Unityctl.Cli -- <command> --project <path> --json`
- Transport: IPC (Named Pipe) + Batch 폴백
- 테스트 프로젝트: `C:\Users\ezen601\Desktop\Jason\robotapp2`

### B. Unity MCP (CoplayDev)
- 패키지: `com.coplaydev.unity-mcp` (이미 robotapp2에 설치됨)
- MCP 서버: StdioBridgeHost (port 6400/6402)
- GitHub: https://github.com/CoplayDev/unity-mcp

## 벤치마크 항목

### 1. 토큰 효율 비교
각 작업에 대해 AI 에이전트가 소비하는 토큰 수 비교:

| 작업 | 측정 방법 |
|------|-----------|
| 에디터 목록 조회 | unityctl: `editor list --json` 출력 크기 vs MCP: tools/list 스키마 크기 |
| 프로젝트 상태 확인 | unityctl: `status --json` vs MCP: 동등 도구 호출 |
| 컴파일 체크 | unityctl: `check --json` vs MCP: 동등 도구 호출 |
| 빌드 실행 | unityctl: `build --json` vs MCP: 동등 도구 호출 |
| 도구 스키마 크기 | unityctl: `schema --format json` 전체 크기 vs MCP: tools/list 전체 크기 |

**측정**: 각 응답의 JSON 크기 (bytes) + 추정 토큰 수 (bytes / 4)

### 2. 응답 시간 비교
Unity Editor가 열린 상태에서 동일 작업의 wall-clock 시간:

| 작업 | 측정 방법 |
|------|-----------|
| ping / 연결 확인 | `time unityctl ping` vs MCP 동등 |
| status 조회 | cold start + warmed 각각 |
| check (컴파일) | 실제 시간 |
| test (EditMode) | 실제 시간 |

**측정**: 각 5회 실행 → 중앙값 (ms)

### 3. 기능 커버리지 비교
| 기능 | unityctl | Unity MCP | 비고 |
|------|----------|-----------|------|
| 각 기능별 지원 여부 체크 | | | |

### 4. CI/CD 호환성
| 시나리오 | unityctl | Unity MCP |
|----------|----------|-----------|
| Editor 없이 동작 | ? | ? |
| Headless batch mode | ? | ? |
| GitHub Actions 통합 | ? | ? |

## 산출물

`docs/benchmark/` 디렉토리에:
1. `benchmark-results.md` — 정량 데이터 테이블 + 분석
2. `benchmark-script.sh` — 재현 가능한 벤치마크 스크립트
3. `token-comparison.md` — 토큰 효율 상세 비교

## 규칙
- 실제 측정값만 사용. 추정/가정 금지.
- MCP 서버가 동작하지 않으면 해당 항목은 "N/A" 표기
- unityctl 명령은 `--json` 플래그 사용
- 결과는 마크다운 테이블로 구조화
- 공정한 비교: 양쪽 모두 warmed state 기준
