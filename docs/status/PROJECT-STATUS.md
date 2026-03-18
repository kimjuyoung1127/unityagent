# unityctl 프로젝트 상태

최종 업데이트: 2026-03-18 (KST)
기준 문서: `CLAUDE.md`, `docs/ref/phase-roadmap.md`, `docs/DEVELOPMENT.md`

## 현재 Phase

- **Phase 0 ~ 2C**: 완료
- **Phase 3B (Flight Recorder)**: 완료
- **Phase 3A (Session Layer)**: 완료
- **Phase 4A (Ghost Mode)**: 완료
- **Phase 3C (Watch Mode)**: 완료 (watch 스트리밍 끊김 버그 수정 중)
- **Phase 4B (Scene Diff)**: 완료
- **Phase 5 (Agent Layer)**: 완료
- **Phase 1C (CI/CD)**: 완료

## 라이브 검증 (robotapp2, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| `ping` | ✅ | IPC + batch 모두 동작 |
| `status` | ✅ | isCompiling, isPlaying, platform 정상 |
| `check` | ✅ | 44 assemblies, scriptCompilationFailed 정상 |
| `build --dry-run` | ✅ | 19개 preflight 항목 검증, OutputPath 문제 정확히 감지 |
| `build` (실제) | ✅ | 프로젝트 에러 정확히 캡처 (AssetDatabase 런타임 사용) |
| `test --mode edit` | ✅ | 410개 실행, 403 pass / 7 fail (프로젝트 자체 실패) |
| `exec --code` | ✅ | IPC로 C# 식 실행 (`Application.version` → "0.1") |
| `schema --format json` | ✅ | 전체 커맨드 스키마 JSON 출력 |
| `session list` | ✅ | 세션 추적 + 기록 정상 |
| `log --stats` | ✅ | NDJSON 로그 기록/쿼리 정상 |
| `scene snapshot` | ⚠️ | 동작하지만 Editor 2개 열려있을 때 라우팅 문제 |
| `watch --channel console` | ❌ | 연결 후 즉시 끊김 (메인 스레드 구독 타이밍 버그, 수정 중) |
| `editor list` | ✅ | 설치된 에디터 자동 탐색 |
| `init` | ✅ | manifest.json 플러그인 설치 |

## Plugin 호환성 수정 (Unity 6)

- `WatchEventSource`: `CompilationFinishedHandler` → `Action<object>` (Unity 6 API 변경)
- `WatchEventSource`: `EditorApplication.CallbackFunction` → `Action`
- `IpcServer`: `Environment.TickCount64` → `(long)Environment.TickCount` (Mono 호환)

## 벤치마크 결과 (median, ms)

| 작업 | dotnet run | published exe | Unityctl.Mcp | CoplayDev MCP |
|------|-----------|---------------|--------------|---------------|
| ping | 2008 | 300 | 100 | 1 |
| editor_state | 2009 | 301 | 100 | 100 |
| active_scene | 2004 | 300 | 100 | 99 |

Unityctl.Mcp resident mode는 CoplayDev와 동등한 100ms대.

## 자동화 검증

| 항목 | 상태 | 비고 |
|------|------|------|
| `dotnet build unityctl.slnx` | ✅ | 경고/오류 없이 통과 |
| `dotnet test unityctl.slnx` | ✅ | 총 304개 테스트 통과 |

| 프로젝트 | 통과 |
|----------|------|
| Unityctl.Shared.Tests | 60 |
| Unityctl.Core.Tests | 96 |
| Unityctl.Cli.Tests | 122 |
| Unityctl.Mcp.Tests | 7 |
| Unityctl.Integration.Tests | 19 |

## 즉시 다음 작업

1. watch 스트리밍 끊김 버그 수정 (메인 스레드 Subscribe 타이밍)
2. scene snapshot Editor 라우팅 문제 확인
3. 벤치마크 리포트 커밋
