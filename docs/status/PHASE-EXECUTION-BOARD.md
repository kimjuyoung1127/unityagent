# Phase 실행 보드

상태값: `Ready | InProgress | QA | Done | Hold`

| phase | module | priority | status | must_read_docs | last_updated |
|-------|--------|----------|--------|----------------|--------------|
| Phase 0 | 프로젝트 골격 | P0 | Done | CLAUDE.md | 2026-03-17 |
| Phase 0.5 | Plugin Bootstrap | P0 | Done | CLAUDE.md | 2026-03-17 |
| Phase 1A | CLI 기본 (Discovery, BatchMode, Retry) | P0 | Done | docs/ref/architecture-mermaid.md | 2026-03-17 |
| Phase 1B | 핵심 기능 (Build, Test, Check Handler) | P0 | Done | docs/ref/code-patterns.md | 2026-03-17 |
| Phase 1C | 테스트 + 배포 | P1 | Done | .github/workflows/ | 2026-03-18 |
| Phase 2A | Foundation (Payload, Core 추출) | P0 | Done | docs/ref/architecture-mermaid.md | 2026-03-17 |
| Phase 2A+ | Tools Metadata | P0 | Done | docs/ref/phase-roadmap.md | 2026-03-17 |
| Phase 2B | IPC Transport (Named Pipe) | P0 | Done | docs/ref/phase-2b-plan.md | 2026-03-17 |
| Phase 2C | Async Commands (TestHandler) | P0 | Done | docs/ref/phase-roadmap.md | 2026-03-18 |
| Phase 3B | Flight Recorder (NDJSON 로깅, CLI 쿼리) | P1 | Done | docs/ref/phase-roadmap.md | 2026-03-18 |
| Phase 3A | Session Layer (상태머신 6개, MCP Tasks 매핑) | P1 | Done | docs/ref/phase-roadmap.md | 2026-03-18 |
| Phase 4A | Ghost Mode (--dry-run preflight, 3단계 검증) | P1 | Done | docs/ref/phase-roadmap.md | 2026-03-18 |
| Phase 3C | Watch Mode (Push 스트리밍, ConcurrentQueue) | P1 | Done | docs/ref/phase-roadmap.md | 2026-03-18 |
| Phase 4B | Scene Diff (SerializedObject, GlobalObjectId) | P2 | Done | docs/ref/phase-roadmap.md | 2026-03-18 |
| Phase 5 | Agent Layer (Unityctl.Mcp 네이티브 서버, schema, exec) | P2 | Done | docs/ref/phase-roadmap.md | 2026-03-18 |
| MCP Hybrid | unityctl_run (allowlist write) + schema filter | P1 | Done | docs/ref/phase-roadmap.md | 2026-03-18 |
| Write C | 커버리지 확장 (Asset/Prefab/Package/Material/Animation/UI) | P1 | Done | docs/ref/phase-roadmap.md | 2026-03-18 |
| Script v1 | script create/edit/delete/validate | P1 | Done | docs/ref/phase-roadmap.md | 2026-03-19 |
| Diagnostics | doctor + IPC 자동 진단 | P1 | Done | docs/ref/phase-roadmap.md | 2026-03-19 |
| Read API P0 | asset/gameobject/component query + hierarchy + build-settings | P0 | Done | docs/ref/phase-roadmap.md | 2026-03-19 |
| P2 Batch Execute | batch execute + Undo transaction rollback (IPC-only v1) | P2 | Done | docs/ref/phase-roadmap.md | 2026-03-19 |
| P3 Screenshot | screenshot capture (Scene/Game View, base64, MCP 전용 도구) | P2 | Done | docs/ref/phase-roadmap.md | 2026-03-19 |
| P0 잔여분 | asset get-labels/set-labels + build-settings set-scenes | P0 | Done | docs/ref/phase-roadmap.md | 2026-03-19 |
| Tags & Layers + Editor Utility | tag list/add, layer list/set, gameobject set-tag/set-layer, console clear/get-count, define-symbols get/set | P0 | Done | docs/ref/phase-roadmap.md | 2026-03-19 |
| Lighting & NavMesh | lighting bake/cancel/clear/get-settings/set-settings + navmesh bake/clear/get-settings (8개 명령) | P4 | Done | docs/ref/phase-roadmap.md | 2026-03-19 |
| Physics Settings | physics get-settings/set-settings/get-collision-matrix/set-collision-matrix (4개 명령) | P4 | Done | docs/ref/phase-roadmap.md | 2026-03-19 |
| Editor Utility 확장 + Script List | editor pause/focus-gameview/focus-sceneview + script list (4개 명령) | P0 | Done | docs/ref/phase-roadmap.md | 2026-03-19 |
| NuGet v0.2.0 배포 | dotnet tool install -g unityctl/unityctl-mcp + GitHub Release 자동화 | P0 | Done | release.yml | 2026-03-19 |
| MCP Context Optimization | C1 QueryTool + C2 Schema Category + C3 Description 경량화 (33→12 MCP 도구) | P0 | Done | docs/ref/phase-roadmap.md | 2026-03-19 |
| Script Patch v2 | script patch (줄 단위 삽입/삭제/교체) | P0 | Done | docs/ref/phase-roadmap.md | 2026-03-19 |
| Script v2 | script-get-errors/find-refs/rename-symbol (진단 + 리팩터링) | P0 | Done | docs/ref/phase-roadmap.md | 2026-03-19 |
| Project Validate | project validate (게임 준비 상태 6개 체크) | P0 | Done | docs/ref/phase-roadmap.md | 2026-03-19 |
| UI Read Slice 1 | ui find/get (UGUI-first inspection) | P1 | Done | docs/ref/phase-roadmap.md | 2026-03-19 |
| UI Interaction Slice 1 | ui toggle/input (deterministic state set) | P1 | Done | docs/ref/phase-roadmap.md | 2026-03-19 |
| Mesh Primitive Create | mesh create-primitive (built-in primitives) | P1 | Done | docs/ref/phase-roadmap.md | 2026-03-19 |
| Multi-Instance Routing Phase 1 | editor current/select + project-path selection fallback + target metadata | P0 | Done | docs/ref/phase-roadmap.md | 2026-03-20 |
| Production Domain Expansion | camera list/get, texture get/set-import-settings, scriptableobject find/get/set-property, shader find/get-properties (9개 명령) | P1 | Done | docs/ref/phase-roadmap.md | 2026-03-20 |
| Visual Verification v2 Phase 1 | workflow verify + projectValidate/capture/imageDiff/consoleWatch/uiAssert/playSmoke | P0 | Done | docs/ref/phase-roadmap.md | 2026-03-20 |

## Zero-Drift 규칙
1. `src/` 구조를 코드 모듈 Source of Truth로 간주한다.
2. 보드의 phase/module 상태와 `docs/status/PROJECT-STATUS.md`는 동기화한다.
3. Phase 완료 시 `CLAUDE.md`의 Phase Status 테이블도 갱신한다.
4. Transport 설계 변경은 `docs/ref/phase-2b-plan.md` (또는 해당 phase 문서) 에 반영한다.
5. 테스트 수는 `PROJECT-STATUS.md`에서만 관리한다. CLAUDE.md에는 하드코딩하지 않는다.
