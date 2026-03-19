# unityctl 전체 Phase 로드맵

> 최종 업데이트: 2026-03-19
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
| Write via MCP | `unityctl_run` (allowlist — `RunTool.cs` 참조) | MCP Hybrid + Write C + Script v1 + P0 잔여분 + Batch Execute v1 | ✅ |
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
Phase 1C  — 테스트 + 배포         ✅ 완료
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
Write C   — 커버리지 확장 (28개 명령)     ✅ 완료
Script v1 — script create/edit/delete/validate  ✅ 완료
Diagnostics — doctor + IPC 자동 진단     ✅ 완료
Read API P0 Slice 1 — asset/gameobject/component query + ExploreTool ✅ 완료
Read API P0 Slice 2 — scene hierarchy + build-settings get-scenes ✅ 완료
Read API P0 Slice 3 — asset reference graph v1 ✅ 완료
P3 Screenshot  — screenshot capture (Scene/Game View, base64, MCP 도구) ✅ 완료
P0 잔여분   — asset get-labels/set-labels + build-settings set-scenes ✅ 완료
Build Profile / Target Control — build-profile list/get-active/set-active + build-target switch ✅ 완료
P2 Batch Execute — batch execute + Undo transaction rollback (IPC-only v1) ✅ 완료
Tags & Layers + Editor Utility — tag/layer/console/define-symbols 10개 명령 ✅ 완료
Lighting & NavMesh — lighting bake/cancel/clear/get-settings/set-settings + navmesh bake/clear/get-settings 8개 명령 ✅ 완료
Physics Settings — physics get-settings/set-settings/get-collision-matrix/set-collision-matrix 4개 명령 ✅ 완료
Editor Utility 확장 + Script List — editor pause/focus-gameview/focus-sceneview + script list 4개 명령 ✅ 완료
```

### 실행 순서 변경 근거

| 변경 | 이유 |
|------|------|
| 3B → 3A | Flight Recorder는 독립적이며, 이후 Phase 디버깅 인프라로 활용. Session은 FlightLog 위에 구축하는 게 자연스러움 |
| 4A → 3C 앞으로 | Ghost Mode는 기존 BuildHandler 재활용이라 범위가 작고 빠름. Watch Mode는 IPC 스트리밍 프로토콜 확장이라 더 복잡 |

---

## 완료된 Phase 상세 (요약)

> 각 Phase의 상세 설계/검증 기록은 [`docs/internal/phase-history.md`](../internal/phase-history.md)에 보존되어 있습니다.

| Phase | 요약 | 상세 |
|-------|------|------|
| **2B** IPC Transport | Named Pipe probe-first, 3 플랫폼, MessageFraming, CommandExecutor 자동 선택 | [상세](../internal/phase-history.md#phase-2b--ipc-transport) |
| **2C** Async Commands | polling, single-flight, ACCEPTED [104], TestResultCollector | [상세](../internal/phase-history.md#phase-2c--async-commands) |
| **3B** Flight Recorder | NDJSON 로깅, 15필드 FlightEntry, `unityctl log` CLI | [상세](../internal/phase-history.md#phase-3b--flight-recorder) |
| **3A** Session Layer | 6개 상태머신, NDJSON 저장소, MCP Tasks 매핑 | [상세](../internal/phase-history.md#phase-3a--session-layer) |
| **4A** Ghost Mode | `build --dry-run` preflight, 3단계 검증 (error/warning/info) | [상세](../internal/phase-history.md#phase-4a--ghost-mode-preflight-validation) |
| **3C** Watch Mode | Push 스트리밍, ConcurrentQueue, 영구 Named Pipe | [상세](../internal/phase-history.md#phase-3c--watch-mode) |
| **4B** Scene Diff | SerializedObject 순회, GlobalObjectId, propertyPath diff | [상세](../internal/phase-history.md#phase-4b--scene-diff) |
| **5** Agent Layer | Unityctl.Mcp 네이티브 서버, schema, exec, workflow | [상세](../internal/phase-history.md#phase-5--agent-layer) |
| **MCP Hybrid** | `unityctl_run` allowlist + `unityctl_schema(command=...)` | [상세](../internal/phase-history.md#mcp-하이브리드-전략) |
| **Write C** | 28개 명령 (Asset/Prefab/Package/Material/Animation/UI) | [상세](../internal/phase-history.md#write-api-phase-c--커버리지-확장) |
| **Read API P0** | asset/gameobject/component + hierarchy + build-settings + reference-graph | [상세](../internal/phase-history.md#read-api-p0-slice-1--assetgameobjectcomponent-query) |
| **P3 Screenshot** | Camera.Render() → base64, Scene/Game View, PNG/JPG | [상세](../internal/phase-history.md#p3--screenshot--visual-feedback) |
| **Build Profile** | build-profile list/get-active/set-active + build-target switch | [상세](../internal/phase-history.md#build-profile--build-target-control) |
| **P2 Batch** | batch execute + Undo transaction rollback (IPC-only v1) | [상세](../internal/phase-history.md#p2--배치-편집트랜잭션) |
| **Tags & Layers** | tag/layer/console/define-symbols 10개 명령 | [상세](../internal/phase-history.md#tags--layers--editor-utility) |
| **Lighting & NavMesh** | lighting 5개 + navmesh 3개 = 8개 명령, 비동기 bake 폴링 | [상세](../internal/phase-history.md#lighting--navmesh) |
| **Physics Settings** | physics 4개 명령, DynamicsManager iterator, 32×32 collision matrix | [상세](../internal/phase-history.md#physics-settings) |
| **Editor Utility 확장 + Script List** | editor pause/focus/script list 4개 명령, MonoScript 탐색 | [상세](../internal/phase-history.md#editor-utility-확장--script-list) |

---

## 다음 개발 로드맵 (경쟁 분석 기반)

> 최종 업데이트: 2026-03-19
> 출처: `docs/internal/root-research.md` — CoplayDev/unity-mcp, unity-editor-mcp 경쟁 분석

| 우선순위 | 영역 | 핵심 내용 |
|---------|------|----------|
| **P0** | 읽기/탐색 API 잔여분 | asset labels, build-settings set-scenes. `gameobject find/get`, `component get`, `asset find/get-info/get-dependencies`, `scene hierarchy`, `build-settings get-scenes`, `asset reference graph`는 완료 |
| **P1** | 멀티 인스턴스 라우팅 | 여러 Unity Editor 동시 제어 + 에이전트 작업 고정 UX |
| ~~**P2**~~ | ~~배치 편집/트랜잭션~~ | ✅ 완료. `batch execute` (IPC-only v1), 부분 실패 rollback, IPC 왕복 수 감소 |
| ~~**P3**~~ | ~~스크린샷/시각 피드백~~ | ✅ 완료. `screenshot capture` (Scene/Game View, base64, PNG/JPG, MCP 전용 도구) |
| ~~**P4**~~ | ~~Graphics/Camera~~ | ✅ 부분 완료. Lighting 5개 + NavMesh 3개 + Physics 4개 = 12개 명령. URP/HDRP Volume, Cinemachine은 미구현 |
| **P5** | 고급 UI 자동화 | UI 찾기/읽기/클릭/입력 시퀀스 |
| **P6** | 스크립트 편집 v2 | text edits, symbol-aware patch, find refs, compile error 자동 수정 |
| **P7** | 전문화 도메인 | texture, shader graph, vfx, audio, terrain, probuilder |

### 병행 과제
- macOS / Linux 실제 테스트
- `dotnet tool` NuGet 패키지 배포
- write API property alias 개선
