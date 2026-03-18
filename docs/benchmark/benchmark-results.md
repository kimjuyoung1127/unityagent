# Benchmark Results

최종 업데이트: 2026-03-18 (KST)

측정 기준:

- 테스트 프로젝트: `C:\Users\ezen601\Desktop\Jason\robotapp2`
- CoplayDev endpoint: `127.0.0.1:6400`
- warmed 기준 8회 측정, median 사용
- `dotnet run` / `published exe`는 per-invocation
- `Unityctl.Mcp`는 resident stdio MCP session

## 응답 시간

| 작업 | dotnet run | published exe | Unityctl.Mcp | CoplayDev bridge |
|------|-----------:|--------------:|-------------:|-----------------:|
| ping | 2015 ms | 304 ms | 100 ms | 1 ms |
| editor_state | 2095 ms | 303 ms | 100 ms | 100 ms |
| active_scene | 2094 ms | 304 ms | 99 ms | 100 ms |
| diagnostic | 2053 ms | 401 ms | 101 ms | 100 ms |

## 해석

- `dotnet run`은 SDK + process startup cost가 지배적이다.
- `published exe`는 `dotnet run` 대비 대략 6~7배 빠르다.
- `Unityctl.Mcp` resident mode는 `editor_state` / `active_scene` 기준 CoplayDev와 사실상 같은 100ms대다.
- `ping`은 CoplayDev bridge가 훨씬 빠른데, 이는 `6400` raw bridge ping이 매우 짧은 경로로 처리되기 때문이다.

## caveat

- clean-state 재실행에서는 cross-project leakage는 관찰되지 않았다.
- 다만 `active_scene`의 path는 양쪽 모두 빈 문자열로 반환되어, scene path 정확성 자체를 비교 근거로 쓰지는 않는다.

## 관련 산출물

- robotapp2 side raw report:
  - `C:\Users\ezen601\Desktop\Jason\robotapp2\docs\benchmark\2026-03-18-unityctl-vs-mcp-extended.md`
  - `C:\Users\ezen601\Desktop\Jason\robotapp2\docs\benchmark\2026-03-18-unityctl-vs-mcp-extended.json`
