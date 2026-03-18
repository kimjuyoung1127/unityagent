# Token Comparison

최종 업데이트: 2026-03-18 (KST)

측정 기준:

- tokenizer 라이브러리가 로컬에 없어 `UTF-8 bytes / 4` 휴리스틱으로 approximate token 계산
- `unityctl`는 published CLI와 resident MCP 둘 다 기록
- CoplayDev는 packaged stdio MCP `tools/list` 기준

## 도구 스키마 크기

| 스택 | 측정 대상 | Bytes | Approx tokens |
|------|-----------|------:|--------------:|
| unityctl CLI | `schema --format json` | 11,927 | 2,982 |
| Unityctl.Mcp | `tools/list` | 5,024 | 1,256 |
| CoplayDev MCP | `tools/list` | 45,705 | 11,427 |

## 단일 왕복 비용

| 스택 | 의도 | Request bytes | Response bytes | Total bytes | Approx tokens |
|------|------|--------------:|---------------:|------------:|--------------:|
| unityctl CLI | status | 133 | 348 | 481 | 121 |
| unityctl CLI | build dry-run | 171 | 4,429 | 4,600 | 1,150 |
| Unityctl.Mcp | status | 106 | 361 | 467 | 117 |
| Unityctl.Mcp | build dry-run | 137 | 4,571 | 4,708 | 1,177 |

## 10회 누적 비용

| 스택 | 의도 | Schema 1회 + 10x request/response bytes | Approx tokens |
|------|------|-----------------------------------------:|--------------:|
| unityctl CLI | status | 16,737 | 4,185 |
| unityctl CLI | build dry-run | 57,927 | 14,482 |
| Unityctl.Mcp | status | 9,694 | 2,424 |
| Unityctl.Mcp | build dry-run | 52,104 | 13,026 |

## 핵심 결론

- `Unityctl.Mcp tools/list`는 CoplayDev `tools/list`보다 약 `9.1x` 작다.
- `unityctl CLI schema`도 CoplayDev `tools/list`보다 약 `3.8x` 작다.
- `status` 계열 왕복은 `unityctl CLI`와 `Unityctl.Mcp`가 거의 같은 payload 크기다.
- CoplayDev `tools/list`에는 direct build tool이 없으므로, "빌드해줘" 의도에서는 큰 schema 비용을 먼저 지불하고도 바로 build action으로 이어지지 않는다.

## CoplayDev build capability

| 항목 | 결과 |
|------|------|
| direct build tool in `tools/list` | ❌ 없음 |
| matching tool names | `none` |

## 관련 산출물

- robotapp2 side raw report:
  - `C:\Users\ezen601\Desktop\Jason\robotapp2\docs\benchmark\2026-03-18-token-efficiency.md`
  - `C:\Users\ezen601\Desktop\Jason\robotapp2\docs\benchmark\2026-03-18-token-efficiency.json`
