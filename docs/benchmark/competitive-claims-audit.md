# Competitive Claims Audit

최종 업데이트: 2026-03-18 (KST)

## 목적

`unityctl vs CoplayDev Unity MCP` 비교 문구 중에서 무엇이 이미 검증됐고, 무엇이 표현을 낮춰야 하며, 무엇이 아직 추가 측정이 필요한지 정리합니다.

강한 포지셔닝은 유지하되, 외부에서 반박당할 수 있는 문장은 줄이는 것이 목적입니다.

## 결론 요약

- `unityctl`의 가장 강한 차별점은 여전히 맞습니다.
  - headless-first
  - schema-driven
  - native .NET MCP
  - IPC to batch fallback
- 하지만 현재 비교표에는 그대로 쓰기엔 위험한 문장이 섞여 있습니다.
  - `5,024 B schema`
  - `CoplayDev 80+ tools`
  - `dotnet run 2초 vs bridge 1ms`
  - `exec 1개 = 도구 80개`

## Claim Audit

| 주장 | 판정 | 근거 | 권장 표현 |
|------|------|------|-----------|
| `unityctl` CLI 명령 18개 | 검증됨 | `CommandCatalog.All`에 18개 등록 | 그대로 사용 가능 |
| `unityctl` MCP top-level tool 12개 | 검증됨 | 11개 tool class에서 12개 `[McpServerTool]` 이름이 등록됨 | `12 MCP tool names across 11 classes` 권장 |
| `schema`가 5,024 B | 수정 필요 | 현재 로컬 실측 `unityctl schema --format json`은 12,386 B | `5,024 B`는 actual MCP `tools/list` 측정이 끝날 때까지 보류 |
| CoplayDev 45,705 B schema | 부분 검증 | 프로젝트 문서에 기록은 있으나 현재 저장소 내 재현 스크립트 없음 | 재현 스크립트/원본 응답 저장 후 사용 |
| CoplayDev 80+ 도구 | 수정 필요 | 공개 README는 top-level tool 39개를 명시 | `39 top-level tools, with many multi-action management tools` 권장 |
| unityctl headless CI/CD | 검증됨 | `check/test/build --dry-run`이 batch 경로와 함께 제품 문서 및 상태 문서에 명시 | 그대로 사용 가능 |
| CoplayDev는 Editor 필수 | 보수 표현 필요 | 공개 quickstart는 Unity 창에서 서버를 켜는 editor-first 흐름 | `public quickstart is editor-first; no documented headless workflow` 권장 |
| native C# SDK vs Python/TS bridge | 검증됨 | `Unityctl.Mcp`는 ModelContextProtocol C# SDK, CoplayDev quickstart는 Python + uv/uvx 설정 명시 | 그대로 사용 가능 |
| IPC probe to batch fallback | 검증됨 | architecture/status 문서와 코드 구조가 일치 | 그대로 사용 가능 |
| `--dry-run` 19개 preflight | 검증됨 | 상태 문서와 preflight 테스트/문서에 명시 | 그대로 사용 가능 |
| Flight Recorder / Session / Watch | unityctl 쪽은 검증됨 | 상태 문서와 커맨드 카탈로그에 존재 | 그대로 사용 가능 |
| CoplayDev에 Watch 없음 | 보수 표현 필요 | 공개 README에 dedicated watch-style streaming command는 보이지 않음 | `no documented dedicated watch-style stream` 권장 |
| exec는 범용 escape hatch | 부분 검증 | 존재는 맞지만 구현은 reflection 기반 단일 식 위주 | `broad escape hatch for many static Unity APIs` 권장 |
| `exec 1개 = 도구 80개` | 수정 필요 | 현재 `ExecHandler`는 체이닝/멀티라인/인스턴스 탐색이 없음 | 마케팅 문구로도 과함. 사용 금지 권장 |
| cold start 2초 vs 1ms | 수정 필요 | `dotnet run` vs resident bridge는 비교 축이 다름. 상태 문서에는 resident 100ms vs 100ms도 있음 | `published exe ~300ms`, `resident MCP ~100ms` 식으로 축을 맞춰 비교 |

## 현재 확인 가능한 로컬 수치

2026-03-18 로컬 측정 기준:

| 항목 | 값 | 측정 방식 |
|------|----|-----------|
| CLI command count | 18 | `unityctl schema --format json` |
| CLI schema size | 12,386 B | `unityctl schema --format json` UTF-8 byte count |
| CLI tools JSON size | 10,199 B | `unityctl tools --json` UTF-8 byte count |
| MCP top-level tool classes | 11 | `src/Unityctl.Mcp/Tools/*Tool.cs` 개수 |
| MCP top-level tool names | 12 | `[McpServerTool(Name = ...)]` 등록 개수 |

측정 스크립트:

- [measure-unityctl-metrics.ps1](./measure-unityctl-metrics.ps1)

## 표현을 이렇게 바꾸면 더 안전함

### 추천

- `unityctl ships 18 first-class CLI commands and 12 native .NET MCP tool names across 11 classes.`
- `unityctl is headless-first: check, test, and build preflight can run without a live Unity Editor.`
- `unityctl uses native .NET MCP hosting, while CoplayDev's public setup is Python/uv-based.`
- `unityctl exposes fewer top-level commands, but pairs them with a schema-first interface and an exec escape hatch.`

### 피해야 함

- `schema is 5,024 B` without a reproducible capture
- `CoplayDev has 80+ tools` without clarifying that this likely includes sub-actions
- `exec 1개 = 도구 80개`
- `CoplayDev cannot do X` when the public evidence only shows `not documented`

## 지금 당장 밀 수 있는 비교 포인트

README 수정 없이도 바로 강화 가능한 항목:

1. 재현 가능한 측정 스크립트 추가
2. 검증된 claim만 남긴 비교 문서 정리
3. actual MCP `tools/list` 캡처 harness 추가
4. published exe 기준 latency 측정 스크립트 추가

## 참고 근거

로컬 소스:

- `src/Unityctl.Shared/Commands/CommandCatalog.cs`
- `src/Unityctl.Mcp/Tools/`
- `src/Unityctl.Plugin/Editor/Commands/ExecHandler.cs`
- `docs/status/PROJECT-STATUS.md`

공개 비교 기준:

- [CoplayDev/unity-mcp README](https://github.com/CoplayDev/unity-mcp)
