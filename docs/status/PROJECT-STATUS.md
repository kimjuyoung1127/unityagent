# unityctl 프로젝트 상태

최종 업데이트: 2026-03-19 (KST)
기준 문서: `CLAUDE.md`, `docs/ref/phase-roadmap.md`, `docs/internal/DEVELOPMENT.md`

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
- **Script Editing v1 (create/edit/delete/validate)**: 완료
- **Diagnostics (doctor + IPC 자동 진단)**: 완료
- **Read API P0 Slice 1 (asset/gameobject/component query + includeInactive + ExploreTool)**: 완료
- **Read API P0 Slice 2 (scene hierarchy + build-settings get-scenes)**: 완료
- **Read API P0 Slice 3 (asset reference graph v1)**: 완료
- **Build Profile / Build Target Control (`build-profile *`, `build-target switch`)**: 완료
- **P3 Screenshot / Visual Feedback**: 완료
- **P2 Batch Execute / Transaction (`batch execute`)**: 완료
- **Tags & Layers + Editor Utility (tag/layer/console/define-symbols 10개 명령)**: 완료
- **Lighting & NavMesh (lighting 5개 + navmesh 3개 = 8개 명령)**: 완료
- **Physics Settings (physics 4개 명령)**: 완료
- **Editor Utility 확장 + Script List (editor pause/focus-gameview/focus-sceneview + script list 4개 명령)**: 완료

- **MCP Context Optimization (C1 QueryTool + C2 Schema Category + C3 Description 경량화)**: 완료

**전체 Phase 완료. 총 70개 write allowlist 명령, 118개 CLI 명령, 12개 MCP 도구 (33→12 통합).**

## 라이브 검증 (최신)

> 이전 슬라이스의 라이브 검증 아카이브는 `docs/internal/DEVELOPMENT.md` "라이브 검증 아카이브" 섹션 참조.

## MCP Context Optimization 라이브 검증 (robotapp, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| `asset-find` (via QueryTool) | ✅ | 50개 Scene asset 반환 |
| `gameobject-find` (via QueryTool) | ✅ | Main Camera 반환 |
| `tag-list` (via QueryTool) | ✅ | 11개 tag 반환 |
| `physics-get-settings` (via QueryTool) | ✅ | gravity, solver 등 30+ 프로퍼티 |
| `scene-hierarchy` (via QueryTool) | ✅ | 씬 트리 정상 |
| `screenshot capture` (via QueryTool) | ✅ | 1920x1080 PNG base64 |
| MCP `tools/list` 12개 도구 | ✅ | 33→12 통합 확인 |
| `unityctl_schema(category=query)` | ✅ | 카테고리 필터 동작 |
| `unityctl_query` allowlist 통과 | ✅ | asset-find 등 허용 |
| `unityctl_query` allowlist 차단 | ✅ | play-mode 등 거부 |

## Editor Utility 확장 + Script List 라이브 검증 (My project, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| `editor pause` (toggle) | ✅ | isPaused: true 반환, 토글 동작 확인 |
| `editor pause --action unpause` | ✅ | isPaused: false 반환 |
| `editor pause --action pause` | ✅ | isPaused: true 반환, 멱등성 확인 |
| `editor focus-gameview` | ✅ | focused: true, Game View 탭 활성화 확인 |
| `editor focus-sceneview` | ✅ | focused: true, Scene View 탭 활성화 확인 |
| `script list` | ✅ | 6182개 MonoScript 탐색 (Packages 포함) |
| `script list --filter Fps --limit 5` | ✅ | FpsText 1개 반환, 이름 필터 정상 |
| `script list --folder Assets --limit 5` | ✅ | Assets 폴더 내 3개 반환, 폴더 필터 정상 |

## Physics Settings 라이브 검증 (My project, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| `physics get-settings` | ✅ | DynamicsManager 전체 프로퍼티 덤프 (gravity, solver, cloth 등 30+ 프로퍼티) |
| `physics set-settings --property m_Gravity --value "[0,-20,0]"` | ✅ | Vector3 gravity 변경 성공 |
| `physics set-settings --property m_Gravity --value "[0,-9.81,0]"` | ✅ | gravity 원복 성공 |
| `physics set-settings --property m_DefaultSolverIterations --value "12"` | ✅ | int 프로퍼티 변경 성공 |
| `physics set-settings --property m_QueriesHitTriggers --value "false"` | ✅ | bool 프로퍼티 변경 성공 |
| `physics get-collision-matrix` | ✅ | 32×32 매트릭스, ignoredPairs 빈 배열 (기본 상태) |
| `physics set-collision-matrix --layer1 8 --layer2 9 --ignore true` | ✅ | TestLayer↔9 충돌 비활성화, confirmed=true |
| `physics get-collision-matrix` (변경 후) | ✅ | ignoredPairs에 layer 8↔9 표시 확인 |
| `physics set-collision-matrix --layer1 8 --layer2 9 --ignore false` | ✅ | 원복, confirmed=true |

## Tags & Layers + Editor Utility 라이브 검증 (My project, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| `tag list` | ✅ | 7개 기본 태그 확인 (Untagged, Respawn, Finish, EditorOnly, MainCamera, Player, GameController) |
| `tag add --name TestTag` | ✅ | 태그 추가 후 재조회 시 8개 확인 |
| `layer list` | ✅ | 32슬롯, 0-7 builtIn 플래그 정상 |
| `layer set --index 8 --name TestLayer` | ✅ | 유저 레이어 설정 후 재조회 확인 |
| `gameobject set-tag` | ✅ | Directional Light → TestTag, Undo 지원 |
| `gameobject set-layer` | ✅ | Directional Light → layer 8 (TestLayer), name→index 변환 정상 |
| `console get-count` | ✅ | 4 logs, 38 warnings, 0 errors 반환 |
| `console clear` | ✅ | 콘솔 클리어 후 42→1 확인 |
| `define-symbols get` | ✅ | Standalone 타겟, 빈 심볼 확인 |
| `define-symbols set --symbols "TEST_SYMBOL;DEBUG_MODE"` | ✅ | 심볼 설정 후 재조회 시 2개 확인 |
| `define-symbols set --symbols ""` | ✅ | 심볼 원복 (domain reload 트리거 확인) |
| `console get-count` (clear 후) | ✅ | 클리어 후 카운트 감소 확인 |
| `gameobject set-layer --layer TestLayer` (이름 기반) | ✅ | layer name → index 자동 변환 정상 |

## P2 Batch Execute Undo Coverage 추가 검증 (My project, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| `material set` direct undo | ✅ | `_BaseColor` 변경 후 `undo`로 `{1,1,1,1}` 복구 |
| `material set-shader` direct undo | ✅ | `Standard` 변경 후 `undo`로 `Universal Render Pipeline/Lit` 복구 |
| `project-settings set` direct undo | ✅ | `editor.m_AssetNamingUsesSpace=false` 후 `undo`로 `True` 복구 |
| `prefab unpack` direct undo | ✅ | `SM_Bld_Apartment_02` unpack 후 `undo`, 후속 `prefab apply` 성공 |
| `batch execute` rollback (`material-set` + `project-settings set`) | ✅ | forced failure 후 material color + setting 모두 원복 |
| `batch execute` rollback (`prefab-unpack`) | ✅ | forced failure 후 `prefab apply` 성공으로 instance 상태 복구 확인 |
| `batch execute` rollback (`asset-create`) | ✅ | forced failure 후 `asset get-info` not found |
| `batch execute` rollback (`asset-copy`) | ✅ | forced failure 후 `asset get-info` not found |
| `batch execute` rollback (`asset-move`) | ✅ | forced failure 후 source restored, destination not found |
| `asset create` direct undo | ⚠️ | 생성은 성공했지만 `undo` 후에도 asset file이 남음 |
| `asset set-labels` direct undo | ⚠️ | labels 변경 후 `undo` 해도 label이 남음 |

검증 메모:

- 이번 추가 검증으로 `batch execute`의 transactional safe subset을 2채널로 다시 정의했다.
- 현재 rollback 보장 범위:
  - Undo-backed: `gameobject-*`, `component-*`, selected `ui-*`, `material-set`, `material-set-shader`, `player-settings`, `project-settings set`, `prefab unpack`
  - Compensation-backed: `asset-create`, `asset-copy`, `asset-move`
- 제외한 범위:
  - `asset-set-labels`, `material-create`, `prefab-create`
- `asset create`는 별도 사용성 버그도 함께 수정했다. 프로젝트 타입(`Assembly-CSharp` 등)도 `asset create --type <TypeName>`으로 찾도록 개선했고, direct undo와 batch compensation을 분리해 검증했다.

## IPC Self-Healing 추가 검증 (My project, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| held pipe connection 1개 + `ping` | ✅ | 20초간 raw pipe hold 상태에서도 IPC 응답 유지 |
| held pipe connection 1개 + `status` | ✅ | 같은 조건에서 batch fallback 없이 `Ready` 응답 유지 |

검증 메모:

- `IpcServer`는 더 이상 accepted connection을 listener thread에서 끝까지 처리하지 않는다.
- connected pipe를 worker로 넘기고 listener는 즉시 다음 instance를 다시 listen 한다.
- 이번 수정으로 probe-first 모델에서 단일 stuck client가 전체 IPC를 막는 현상을 완화했다.

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
| `dotnet build unityctl.slnx -c Release` | ✅ | 경고/오류 없이 통과 |
| `dotnet test tests/Unityctl.Shared.Tests -c Debug` | ✅ | 64 통과 |
| `dotnet test tests/Unityctl.Core.Tests -c Release` | ✅ | 108 통과 |
| `dotnet test tests/Unityctl.Cli.Tests -c Debug` | ✅ | 302 통과 |
| `dotnet test tests/Unityctl.Mcp.Tests -c Debug` | ✅ | 22 통과 |
| `dotnet test unityctl.slnx -c Release` | ⚠️ | Integration/환경 락, AppLocker 등 워크스테이션 조건에 따라 개별 프로젝트 실행이 더 안정적 |

| 프로젝트 | 통과 |
|----------|------|
| Unityctl.Shared.Tests | 64 |
| Unityctl.Core.Tests | 108 |
| Unityctl.Cli.Tests | 326 |
| Unityctl.Mcp.Tests | 22 |
| Unityctl.Integration.Tests | 23 (환경 의존 2개 실패 가능) |

테스트 인벤토리 기준 합계는 **543개**다 (MCP Context Optimization 테스트 포함, Integration 기존 인벤토리 기준).

신규 자동 검증:

- `Unityctl.Mcp.Tests`에 built `unityctl-mcp.exe` 기준 `initialize` / `tools/list` / `unityctl_schema` / `unityctl_run` allowlist/parameters / invalid tool / missing arg black-box 테스트 추가 (16개)
- `Unityctl.Integration.Tests`에 repo-contained `SampleUnityProject` 기반 closed-editor `status` / `check` / `test --mode edit` / `build --dry-run` 검증 추가
- `Unityctl.Shared.Tests`에 `ExecHandler` 실제 grammar/security/parse contract 테스트 추가
- `.github/workflows/ci-dotnet.yml`에 published CLI smoke (`--help`, `schema`, `tools --json`) 추가

> 이전 실측 상세/경쟁 분석 아카이브 → `docs/internal/DEVELOPMENT.md` "라이브 검증 아카이브" 섹션 참조.

## NuGet v0.2.0 배포 완료 (2026-03-19)

| 항목 | 결과 |
|------|------|
| `dotnet pack` (CLI) | ✅ `unityctl.0.2.0.nupkg` (666KB) |
| `dotnet pack` (MCP) | ✅ `unityctl-mcp.0.2.0.nupkg` (1.6MB) |
| `dotnet tool install -g unityctl` | ✅ NuGet.org 배포 완료 |
| `dotnet tool install -g unityctl-mcp` | ✅ NuGet.org 배포 완료 |
| GitHub Actions release.yml | ✅ 4플랫폼 빌드 + NuGet push + GitHub Release 자동화 |
| GitHub Release v0.2.0 | ✅ Win/Mac(x64+arm64)/Linux 바이너리 첨부 |

설치: `dotnet tool install -g unityctl && dotnet tool install -g unityctl-mcp`

## 후속 과제

1. macOS / Linux 실제 테스트
2. write API property alias 개선 (`mass` → `m_Mass` 등)
3. **다음 개발 로드맵**: `docs/ref/phase-roadmap.md` "다음 개발 로드맵" 섹션 참조

## Transport Latency 실측 (published exe, IPC, 2026-03-18)

| 명령 | Median | Min | Max |
|------|--------|-----|-----|
| `ping` | 528ms | 481ms | 592ms |
| `status` | 523ms | 491ms | 593ms |
| `check` | 668ms | 605ms | 726ms |
| `exec` | 567ms | 499ms | 662ms |

- 첫 실행 JIT 오버헤드: ~1779ms (ping 기준)
- MCP resident mode: ~100ms (프로세스 시작 + probe 없음)
- CLI published exe 오버헤드: ~400ms (프로세스 시작 ~200ms + IPC probe ~100ms + 기타 ~100ms)
