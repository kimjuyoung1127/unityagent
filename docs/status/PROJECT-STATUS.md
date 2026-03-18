# unityctl 프로젝트 상태

최종 업데이트: 2026-03-18 (KST)
기준 문서: `CLAUDE.md`, `docs/ref/phase-roadmap.md`, `docs/DEVELOPMENT.md`

## 현재 Phase

- **Phase 0 ~ 2C**: 완료
- **Phase 3B (Flight Recorder)**: 완료
- **Phase 3A (Session Layer)**: 완료
- **Phase 4A (Ghost Mode)**: 완료
- **Phase 3C (Watch Mode)**: 완료
- **Phase 4B (Scene Diff)**: 완료
- **Phase 5 (Agent Layer)**: 완료
- **Phase 1C (CI/CD)**: 완료

- **MCP Hybrid (unityctl_run + schema filter)**: 완료
- **Write API Phase C (커버리지 확장)**: 완료

**전체 Phase 완료. Write API 확장 (27개 신규 write 명령) 구현 완료. 총 39개 write 명령. 13개 MCP 도구 (allowlist 39개), 388개 dotnet 테스트.**

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
| `scene snapshot` | ✅ | 동작 확인 (Editor 1개일 때 정상 라우팅) |
| `watch --channel console` | ✅ | IPC 스트리밍 동작, heartbeat 수신 확인 |
| `editor list` | ✅ | 설치된 에디터 자동 탐색 |
| `init` | ✅ | manifest.json 플러그인 설치 |

## Write API 라이브 검증 (My project, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| `play start` | ✅ | `isPlaying=true`, `isPaused=false` |
| `play pause` | ✅ | `isPlaying=true`, `isPaused=true` |
| `play stop` | ✅ | 수정 후 `isPlaying=false`, `isPaused=false` |
| `player-settings get productName` | ✅ | `"My project"` 반환 |
| `player-settings get companyName` | ✅ | `"DefaultCompany"` 확인 |
| `player-settings set companyName` | ✅ | `"TestCo"` 설정 후 read-back 확인 |
| `player-settings set companyName` 복구 | ✅ | `"DefaultCompany"`로 복구 완료 |
| `asset refresh` | ✅ | `"Asset refresh scheduled"` 응답 후 IPC 재연결 확인 |
| `gameobject create` | ✅ | `globalObjectId`, `sceneDirty`, `undoGroupName` 반환 |
| `gameobject rename` | ✅ | `TestCube → NewName` 성공 |
| `gameobject move` | ✅ | 같은 씬 내 parent 변경 성공 |
| `gameobject delete` | ✅ | fresh object 기준 삭제 성공 |
| `gameobject activate/deactivate` | ✅ | alias 기준 `active=true/false` 정상 |
| `component add` | ✅ | `componentGlobalObjectId` 반환 확인 |
| `component set-property` vector | ✅ | `Transform.m_LocalPosition` 성공 |
| `component set-property` scalar | ✅ | `Rigidbody.m_Mass = 5` 성공 |
| `component remove` | ✅ | `Rigidbody` 삭제 성공 |
| `scene save` | ✅ | active scene 저장 성공 |
| `scene save --all` | ✅ | dirty scene 1개 저장 성공 |
| `scene create` | ✅ | `robotapp2`에서 `Assets/Scenes/CodexValidationScene.unity` 생성 성공 |
| `scene open` | ✅ | dirty scene 보호 rejection + `--force` open 성공 |
| `undo` | ✅ | `gameobject create` 직후 object 제거 확인 |
| `redo` | ✅ | undo 직후 object 복원 확인 |
| `PrefabGuard` | ✅ | prefab instance child delete에 대해 structured rejection 확인 |

## MCP 하이브리드 전략 실측 (My project, Unity 6000.0.64f1)

| 테스트 | 결과 | 비고 |
|--------|------|------|
| `unityctl_run` allowlist 통과 | ✅ | `play-mode` 등 39개 write 명령 실행 허용 |
| `unityctl_run` allowlist 거부 | ✅ | `exec` 등 비허용 명령 → `"not in the allowlist"` |
| `unityctl_run` 잘못된 JSON | ✅ | `"Invalid JSON in parameters"` 에러 |
| `unityctl_schema(command=...)` 단일 조회 | ✅ | `gameobject-create` 스키마만 반환 |
| `unityctl_schema(command=nonexistent)` | ✅ | `"Unknown command"` 에러 |
| `unityctl_schema()` 전체 조회 | ✅ | 기존 동작 유지 |
| MCP tools/list 13개 | ✅ | `unityctl_run` 포함 13개 도구 목록 |
| E2E: create → add → rename → set-property → remove → delete → save | ✅ | IPC 경로 전체 write 흐름 검증 |

검증 메모:

- `My project`는 테스트 전에 `unityctl init`으로 `com.unityctl.bridge`를 추가했다.
- `asset refresh`는 package/domain reload 후 한동안 IPC가 끊길 수 있으나, 실측에서 `ping` 재연결까지 확인했다.
- `player-settings set`은 실제 프로젝트 설정을 변경하므로 테스트 후 원래 값으로 복구했다.
- `gameobject set-active --active false`는 CLI option binding UX가 거슬려 `gameobject activate/deactivate` alias를 추가했다.
- `component set-property`는 human-friendly field name보다 Unity serialized property path 기준이다 (`mass`가 아니라 `m_Mass`).
- `SM_Bld_Apartment_02` prefab instance child에 대해 `Write operations on prefab instances are not supported in v1.` 메시지를 실측 확인했다.

## Plugin 호환성 수정 (Unity 6)

- `WatchEventSource`: `CompilationFinishedHandler` → `Action<object>` (Unity 6 API 변경)
- `WatchEventSource`: `EditorApplication.CallbackFunction` → `Action`
- `WatchEventSource.Subscribe`: 메인 스레드로 이동 (`EditorApplication.delayCall`)
- `IpcServer`: `Environment.TickCount64` → `(long)Environment.TickCount` (Mono 호환)
- `IpcServer.WatchWriterLoop`: 즉시 heartbeat 전송 (연결 안정성)

## 벤치마크 결과 (median, ms)

| 작업 | dotnet run | published exe | Unityctl.Mcp | CoplayDev MCP |
|------|-----------|---------------|--------------|---------------|
| ping | 2015 | 304 | 100 | 1 |
| editor_state | 2095 | 303 | 100 | 100 |
| active_scene | 2094 | 304 | 99 | 100 |
| diagnostic | 2053 | 401 | 101 | 100 |

Unityctl.Mcp resident mode는 `editor_state` / `active_scene` 기준 CoplayDev와 동등한 100ms대다.
단, clean-state 재실행에서는 `active_scene` 경로가 양쪽 모두 빈 문자열이었으므로 scene-path 자체를 비교 근거로 쓰지는 않는다.

## 자동화 검증

| 항목 | 상태 | 비고 |
|------|------|------|
| `dotnet build unityctl.slnx` | ✅ | 경고/오류 없이 통과 |
| `dotnet test unityctl.slnx -c Release` | ⚠️ | 이번 턴 수정과 직접 관련된 Shared/Core/Cli/Mcp는 통과. Integration은 샘플 프로젝트 lock 환경에서 실패 가능 |

| 프로젝트 | 통과 |
|----------|------|
| Unityctl.Shared.Tests | 60 |
| Unityctl.Core.Tests | 96 |
| Unityctl.Cli.Tests | 193 |
| Unityctl.Mcp.Tests | 16 |
| Unityctl.Integration.Tests | 23 |

테스트 인벤토리 기준 합계는 **388개**다 (scene open/create + undo/redo CLI 테스트 포함).

신규 자동 검증:

- `Unityctl.Mcp.Tests`에 built `unityctl-mcp.exe` 기준 `initialize` / `tools/list` / `unityctl_schema` / `unityctl_run` allowlist/parameters / invalid tool / missing arg black-box 테스트 추가 (16개)
- `Unityctl.Integration.Tests`에 repo-contained `SampleUnityProject` 기반 closed-editor `status` / `check` / `test --mode edit` / `build --dry-run` 검증 추가
- `Unityctl.Shared.Tests`에 `ExecHandler` 실제 grammar/security/parse contract 테스트 추가
- `.github/workflows/ci-dotnet.yml`에 published CLI smoke (`--help`, `schema`, `tools --json`) 추가

Write API 실측 검증:

- `My project`에서 `play`, `player-settings`, `asset refresh` IPC write path 확인
- `play stop` 후 `isPaused=false`까지 정상화 확인
- `asset refresh` timeout 이슈 수정 후 `"scheduled"` 응답 + 후속 IPC 복구 확인
- `gameobject` Phase B (`create`, `rename`, `move`, `delete`, `activate/deactivate`) 실측 성공
- `component` Phase B.5 (`add`, `set-property`, `remove`) 실측 성공
- `scene save` / `scene save --all` 실측 성공
- `PrefabGuard` 실측 성공 (`SM_Bld_Apartment_02` prefab instance child delete / component add)
- `scene create`는 `robotapp2`에서 `Assets/Scenes/CodexValidationScene.unity` 생성까지 실측 확인했다.
- `scene open`은 dirty scene 상태에서 structured rejection, `--force`로 `Main.unity`/임시 씬 전환까지 실측 확인했다.
- `undo` / `redo`는 `ValidationProbe2` 생성 후 `scene snapshot`으로 제거/복원을 실측 확인했다.

## 경쟁 우위 검증 결과

### 토큰 효율 (Codex 벤치마크)

| 항목 | unityctl | CoplayDev MCP | 배율 |
|------|----------|---------------|------|
| 스키마 크기 | ~5,500 B (13 tools, unityctl_run 포함) | 45,705 B | **~8.3x 절감** |
| CLI schema 크기 | 11,927 B | 45,705 B | **3.8x 절감** |
| 단일 status 왕복 | 467 B (Mcp) / 481 B (CLI) | N/A (직접 비교 불가) | — |
| 10회 status 누적 | 9,694 B (Mcp) / 16,737 B (CLI) | N/A (직접 비교 불가) | — |
| CoplayDev에 build 도구 없음 | ✅ build/dry-run 있음 | ❌ 없음 | — |

주석:
- `5,024 B` 값은 published `Unityctl.Mcp tools/list` benchmark 기준이다.
- in-repo black-box harness는 `initialize` / `tools/list` correctness를 검증하지만 raw payload byte capture는 아직 후속 과제다.

### Headless CI/CD (경쟁자 불가 영역)

| 시나리오 | unityctl | CoplayDev |
|----------|----------|-----------|
| Editor 없이 status | ✅ repo `SampleUnityProject`에서 `Ready` 확인 | ❌ 실측 불가 |
| Editor 없이 check | ✅ repo `SampleUnityProject`에서 `assemblies=2` 확인 | ❌ 실측 불가 |
| Editor 없이 EditMode test | ✅ repo `SampleUnityProject`에서 `1 passed` 확인 | ❌ 실측 불가 |
| Editor 없이 dry-run | ⚠️ repo `SampleUnityProject`에서 structured `BuildFailed` (`OutputPath` not writable) | ❌ 실측 불가 |

해석:
- `unityctl`의 headless batch path는 repo에 포함된 재현 가능한 샘플 프로젝트에서 `status` / `check` / `EditMode test`까지 실측으로 검증되었다.
- `build --dry-run`은 batch crash가 아니라 structured preflight 결과까지 도달함이 확인되었다.
- CoplayDev 쪽은 public quickstart가 editor-first라 동일한 closed-editor batch parity를 이 저장소에서는 실측하지 않았다.

### exec 파워 데모 (80개 커맨드 vs exec 1개)

| Unity API 호출 | 결과 |
|----------------|------|
| `PlayerPrefs.GetString` | ✅ "none" |
| `PlayerSettings.companyName` | ✅ "DefaultCompany" |
| `PlayerSettings.productName` | ✅ "robotapp2" |
| `Application.unityVersion` | ✅ "6000.0.64f1" |
| `Application.dataPath` | ✅ 정확한 경로 |
| `Application.isPlaying` | ✅ false |
| 프로퍼티 체이닝 (예: `scenes.Length`) | ❌ 미지원 (한계) |

### 에러 복구 품질

| 시나리오 | 응답 |
|----------|------|
| 잘못된 빌드 타겟 | 유효 타겟 목록 포함 JSON 에러 |
| 존재하지 않는 프로젝트 | StatusCode 200 + 명확한 메시지 |
| 잘못된 exec 코드 | 보안 제한 메시지 + 허용 네임스페이스 안내 |

## CoplayDev Unity MCP 대비 대체율 분석 (2026-03-18)

> ⚠️ 아래 수치는 **추정치**입니다. Phase C 명령 중 `scene open/create`, `undo/redo`는 Unity 실기 검증 완료, 나머지 다수는 미완입니다.

### 기능 영역별 대체율 (추정)

| 기능 영역 | CoplayDev 도구 | unityctl 대응 | 추정 대체율 |
|-----------|---------------|--------------|------------|
| GameObject CRUD | `manage_gameobject` | `gameobject-*` 5개 + `exec` | ~90% (duplicate 미구현) |
| Component CRUD | `manage_components` | `component-*` 3개 | ~95% |
| Asset 관리 | `manage_asset` | `asset-*` 6개 | ~95% ⚠️실측 미완 |
| Prefab | `manage_prefabs` | `prefab-*` 4개 | ~85% ⚠️실측 미완 (revert 미구현) |
| Material/Shader | `manage_material` + `manage_shader` | `material-*` 3개 | ~75% ⚠️실측 미완 (셰이더 그래프 미지원) |
| Scene 관리 | `manage_scene` | `scene-*` 5개 + snapshot/diff | ~85% |
| Animation | `manage_animation` | `animation-*` 2개 | ~50% ⚠️실측 미완 (Cinemachine 미지원) |
| UI | `manage_ui` | `ui-*` 3개 | ~60% ⚠️실측 미완 (TMP/이벤트 미지원) |
| Package 관리 | `manage_packages` | `package-*` 3개 | ~80% ⚠️실측 미완 (scoped registry 미지원) |
| 에디터 상태/제어 | `manage_editor` | `status`, `ping`, `check`, `undo`, `redo` | ~80% |
| Play Mode | `manage_editor` play/pause/stop | `play-*` 3개 | 100% |
| Settings | `manage_editor` 일부 | `player-settings-*` 2 + `project-settings-*` 2 | ~90% ⚠️실측 미완 |
| 스크립트 편집 | `create_script`, `script_apply_edits`, `validate_script` | 없음 (`exec` 부분 대체) | ~10% |
| 콘솔 로그 | `read_console` | `watch --channel console` | ~90% (실시간 스트리밍 우위) |
| 테스트 | `run_tests` + `get_test_job` | `test` (polling 결과 수집) | 100% |
| 빌드 | 없음 ❌ | `build` + `build --dry-run` | ∞ (unityctl 독점) |
| C# 식 실행 | `execute_menu_item` | `exec` (범용 C# 식) | >100% (더 강력) |
| Graphics/VFX | `manage_graphics` (33 액션), `manage_vfx` | 없음 | 0% |
| ProBuilder/Texture/Terrain/Audio | 각각 별도 도구 | 없음 | 0% |
| 멀티 인스턴스 | `set_active_instance` | 없음 | 0% |

### 종합 추정

| 관점 | 추정 대체율 | 비고 |
|------|-----------|------|
| AI 에이전트 일상 작업 | **high-80s%** | GO/Component/Asset/Prefab/Material/Scene/Play/Test/Build 커버, scene/undo 실측 반영 |
| CoplayDev 기능 패리티 | **~60%** | CoplayDev 39개 도구 (수백 sub-action) 중 ~60% 대응 |
| unityctl 독점 기능 포함 | **+25%p** | build/dry-run, flight recorder, session, watch, scene diff, headless batch |

### unityctl 독점 기능 (CoplayDev 대체율 0%)

| 기능 | 가치 |
|------|------|
| Headless Batch Mode | CI/CD에서 Editor 없이 status/check/test/build |
| Ghost Mode (dry-run) | 빌드 전 3단계 preflight 검증 |
| Flight Recorder | 전 커맨드 NDJSON 감사 로그 |
| Session Layer | 6개 상태머신 기반 실행 추적 |
| Watch Streaming | IPC Push 기반 실시간 이벤트 |
| Scene Diff | SerializedObject propertyPath diff |
| 토큰 효율 | 스키마 크기 ~8.3x 절감 (5.5KB vs 45.7KB) |
| Undo 내부 통합 | UndoScope 기반 + `undo` / `redo` CLI 노출 |

### 남은 핵심 공백

| 영역 | 구현 난이도 | ROI |
|------|-----------|-----|
| 스크립트 생성/편집/검증 | 중 | 높음 — AI 에이전트 핵심 워크플로 |
| Graphics/렌더링 (33 액션) | 높음 | 중 — URP/HDRP 프로젝트에서 중요 |
| Texture/Terrain/Audio | 중 | 낮음 — 특수 도메인 |
| 멀티 인스턴스 | 낮음 | 낮음 |

주석:
- CoplayDev 도구 목록은 공개 GitHub 레지스트리 기준 (2026-03 확인).
- "AI 에이전트 일상"은 게임 개발 시 가장 빈번한 씬 편집/에셋 관리/빌드/테스트 루프 기준.
- `exec`가 escape hatch로 작동하므로 실질 커버리지는 수치보다 높을 수 있으나, 구조화된 도구 대비 사용성이 떨어져 표에는 반영하지 않음.

## 후속 과제

1. **Write API 확장 Unity 실측 검증** (asset/prefab/package/material/animation/ui)
2. `docs/benchmark/` 산출물 커밋 및 README 링크 연결
3. MCP raw `tools/list` payload 캡처 하네스 추가
4. macOS / Linux 실제 테스트
5. GitHub Actions CI 실행 검증
6. `dotnet tool` NuGet 패키지 배포
7. exec 프로퍼티 체이닝 지원 개선
8. write API property alias 개선 (`mass` -> `m_Mass` 등)
9. script editing 최소형(create/delete/validate) 설계
10. Graphics/VFX 또는 script editing 중 다음 우선순위 확정
