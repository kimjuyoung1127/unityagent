# unityctl 전체 Phase 로드맵

> 최종 업데이트: 2026-03-20
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
MCP Context Optimization — C1 QueryTool + C2 Schema Category + C3 Description 경량화 (33→12 MCP 도구) ✅ 완료
Script Patch v2 — script patch (줄 단위 삽입/삭제/교체) ✅ 완료
Script v2 — script-get-errors/find-refs/rename-symbol (진단+리팩터링) ✅ 완료
Project Validate — project validate (게임 준비 상태 6개 체크) ✅ 완료
UI Read Slice 1 — ui find/get (UGUI-first inspection) ✅ 완료
UI Interaction Slice 1 — ui toggle/input (deterministic state set) ✅ 완료
Mesh Primitive Create — mesh create-primitive (built-in primitives) ✅ 완료
Multi-Instance Routing Phase 1 — editor current/select (`--project`, `--pid`) + editor instances + project-path selection fallback + target metadata ✅ 완료
Production Domain Expansion — camera list/get + texture get/set-import-settings + scriptableobject find/get/set-property + shader find/get-properties (9개 명령) ✅ 완료
Visual Verification v2 Phase 1 — workflow verify + projectValidate/capture/imageDiff/consoleWatch/uiAssert/playSmoke ✅ 완료
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
| **MCP Context Optimization** | C1 QueryTool (22 read 통합) + C2 Schema Category + C3 Description 경량화, 33→12 MCP 도구 | — |
| **Project Validate** | project validate, compile/buildScenes/camera/light/console/editorState 6개 체크 | — |
| **UI Read / Interaction** | ui find/get + ui toggle/input, UGUI-first inspection과 deterministic state set | — |
| **Mesh Primitive Create** | mesh create-primitive, built-in primitive blockout + Undo | — |
| **Multi-Instance Routing Phase 1** | editor current/select (`--project`, `--pid`), editor instances, project-path selection fallback, target project/pipe/editor metadata | — |
| **Production Domain Expansion** | camera list/get, texture get/set-import-settings, scriptableobject find/get/set-property, shader find/get-properties (9개 명령) | — |
| **Visual Verification v2 Phase 1** | workflow verify, artifact-first output, projectValidate + capture + imageDiff + consoleWatch + uiAssert + playSmoke | — |

---

## 다음 개발 로드맵 (경쟁 분석 기반)

> 최종 업데이트: 2026-03-20
> 출처: `docs/internal/competitive-analysis-2026-03-19.md`

전체 core phase는 완료됐다. 다음 단계는 "MCP 호환 기능 추가" 자체보다, `unityctl`을 **production-safe Unity automation control plane**으로 차별화하는 것이다.

| 우선순위 | 영역 | 핵심 내용 |
|---------|------|----------|
| **P0** | 멀티 인스턴스 라우팅 Phase 2 | running process inventory, true editor-instance identity, session/task별 editor pin 고도화, response metadata 확장 |
| **P0** | Workflow 번들 도구 | raw command 위에 `workflow compile-fix`, `workflow ui-smoke`, `workflow build-verify` 같은 멀티스텝 자동화 계층 추가 |
| **P0** | Visual Verification v2 Phase 2 | play-mode end-of-frame capture/diff, UI click helper, richer console/hierarchy delta evidence 묶음 |
| **P1** | 제작 도메인 확장 | ~~packages~~ ✅, ~~camera~~ ✅, ~~ScriptableObject~~ ✅, ~~texture import~~ ✅, ~~shader~~ ✅ / 남은 영역: Cinemachine, URP/HDRP volume + renderer features, shader graph |
| **P1** | 배포/온보딩 단순화 | plugin release artifact 중심 설치, `init` 자동화 강화, `doctor` 연계 one-shot onboarding, sample workflow/config 보강 |
| **P1** | CI / Cloud bridge | Unity Build Automation, GitHub Actions 등 외부 빌드 시스템과 연계하는 local-to-CI workflow 정리 |
| **P2** | 고급 UI 자동화 | 현재 `ui find/get`, `ui toggle/input` 이후 단계로 실제 click, 검증 assertion, 런타임 시나리오 재생 지원 |
| **P2** | 전문화 도메인 | mesh blockout 이후 texture, VFX, audio, terrain, ProBuilder 등 특정 팀 체감이 큰 surface 확장 |

### 전략 원칙

- 도구 개수 경쟁보다 재현성, rollback, diagnostics, evidence를 우선한다.
- Unity AI와는 범용 assistant UX가 아니라 "검증 가능한 실행 계층"으로 차별화한다.
- Unity Build Automation과는 경쟁보다 연결을 택한다.

### 병행 과제
- macOS / Linux 실제 테스트
- plugin 설치 artifact 경로 단순화
- write API property alias 개선
- workflow/evidence 설계 시 MCP 토큰 비용 우위 유지

---

## MCP 컨텍스트 최적화 로드맵

> 목표: 경쟁 도구(200+ tools) 대비 LLM 토큰 비용 우위를 극대화
> 추가: 2026-03-19

| 순위 | 항목 | 설명 | 효과 |
|------|------|------|------|
| ~~**C1**~~ | ~~Read 도구 통합 (`unityctl_query`)~~ | ✅ 완료. 22개 read 도구를 `unityctl_query` 1개로 통합 (allowlist 27개 read 명령) → MCP 도구 33→12개 | 매 턴 tool 스키마 토큰 대폭 절감 |
| ~~**C2**~~ | ~~Schema 카테고리 필터~~ | ✅ 완료. `unityctl_schema(category: "query")` — 카테고리별 부분 조회 | LLM이 127개 전체를 안 봐도 됨 |
| ~~**C3**~~ | ~~Tool Description 경량화~~ | ✅ 완료. 12개 도구 모두 40자 이내 한 줄 요약, 상세는 `unityctl_schema`로 on-demand | 매 턴 수백 토큰 절감 |
| **C4** | Workflow 번들 도구 | 멀티스텝 패턴(찾기→수정)을 1회 호출로 묶는 `unityctl_quick` | 멀티턴→싱글턴, 총 API call 감소 |
