# 경쟁력 개선 플랜 (Competitive Improvement Plan)

> 작성일: 2026-03-19
> 목적: 경쟁사 대비 unityctl 갭 분석 및 개선 로드맵

---

## 1. 경쟁 환경 요약

### 주요 경쟁자

| 경쟁자 | Stars | 기술 스택 | MCP 도구 | 핵심 차별점 |
|--------|-------|----------|----------|------------|
| [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) | ~7,081 | Python (uv) + WebSocket | 25+ (manage_* 패턴) | 시장 1위, Asset Store 등록, SIGGRAPH 논문 |
| [IvanMurzak/Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) | ~1,300 | .NET (ASP.NET+SignalR) | 52 tools + 48 prompts | 런타임(인게임) AI, Reflection 기반 동적 도구 |
| [CoderGamester/mcp-unity](https://github.com/CoderGamester/mcp-unity) | ~1,400 | Node.js + WebSocket | ~10 + 6 resources | 멀티 클라이언트 지원 |
| [yucchiy/UniCli](https://github.com/yucchiy/UniCli) | 신규 | Go 바이너리 + UPM | 80+ CLI 명령 | Claude Code 마켓플레이스 플러그인, Codex 스킬 |
| [youngwoocho02/unity-cli](https://github.com/youngwoocho02/unity-cli) | 신규 | 단일 바이너리 + HTTP | ~10 | `[UnityCliTool]` 커스텀 도구, Profiler |
| Unity 공식 (com.unity.ai.assistant) | N/A | Named Pipe/Unix Socket | 동적 | Unity 6+ 전용, Closed-source, AI Gateway |

### unityctl 현재 포지셔닝

**"검증 가능한 실행 계층 (Verifiable Execution Layer)"**

경쟁자 전원 대비 고유 우위:
- Headless CI/CD (batch fallback)
- Scene Diff (PropertyPath 수준)
- Flight Recorder (NDJSON 감사 로그)
- Ghost Mode (13항목 preflight)
- Watch Mode (Domain Reload 생존 스트리밍)
- Batch Execute + Undo Rollback
- 네이티브 .NET MCP (외부 런타임 불필요)
- 12개 통합 MCP 도구 (토큰 효율 ~9x)

---

## 2. 갭 분석: 경쟁자가 하는데 우리가 못 하는 것

### GAP-1: 커뮤니티 가시성 & 배포 채널

| 항목 | CoplayDev | IvanMurzak | UniCli | unityctl |
|------|-----------|------------|--------|----------|
| GitHub Stars | 7,081 | 1,300 | 신규 | 소규모 |
| Unity Asset Store | 등록됨 | - | - | 미등록 |
| MCP 레지스트리 | mcpservers.org 등록 | - | - | 미등록 |
| Claude Code 마켓플레이스 | - | - | 등록됨 | 미등록 |
| Discord 커뮤니티 | 활발 | 활발 | - | 없음 |

**영향도:** 최상 — 기술 우위가 인지도로 전환되지 않으면 의미 없음

### GAP-2: C# Reflection 탐색/호출

| 항목 | IvanMurzak | CoplayDev | unityctl |
|------|-----------|-----------|----------|
| Reflection 메서드 탐색 | `reflection-method-find` (private 포함) | `unity_reflect` | 없음 |
| 동적 메서드 호출 | `reflection-method-call` | - | `exec` (단일 식 한정) |
| API 클래스/프로퍼티 조회 | 지원 | 지원 | 없음 |

**영향도:** 상 — AI 에이전트의 자기 학습/탐색 능력 핵심

### GAP-3: URP/HDRP Volume & Post-Processing

| 항목 | CoplayDev v9.5.3 | unityctl |
|------|-------------------|----------|
| Volume Profile CRUD | `manage_graphics` (33 액션) | 없음 |
| Post-Processing 설정 | 지원 | 없음 |
| Renderer Features 관리 | 지원 | 없음 |
| Rendering Stats 쿼리 | 지원 | 없음 |
| Light Baking 설정 | 지원 | `lighting get-settings/set-settings` (있음) |

**영향도:** 상 — URP/HDRP는 현대 Unity 프로젝트 대부분이 사용

### GAP-4: Camera/Cinemachine

| 항목 | CoplayDev v9.5.2 | unityctl |
|------|-------------------|----------|
| Camera 속성 get/set | 지원 | 없음 (project-validate에서 존재 확인만) |
| Cinemachine VirtualCamera | 프리셋, Priority, Noise, Blending | 없음 |
| Cinemachine Brain | 설정 | 없음 |

**영향도:** 중 — 시네마틱/카메라 워크플로우에 필수

### GAP-5: Unity API 문서 통합

| 항목 | CoplayDev v9.5.4 | unityctl |
|------|-------------------|----------|
| ScriptReference 조회 | `unity_docs` | 없음 |
| Manual 조회 | `unity_docs` | 없음 |
| Package Docs 조회 | `unity_docs` | 없음 |

**영향도:** 중 — AI가 API 사용법을 자체 확인하면 정확도 향상

### GAP-6: 커스텀 도구 등록 메커니즘

| 항목 | IvanMurzak | youngwoocho02 | unityctl |
|------|-----------|---------------|----------|
| 어트리뷰트 기반 등록 | `[McpPluginTool]` | `[UnityCliTool]` | 없음 |
| 사용자 확장 | 한 줄로 MCP 도구화 | 한 줄로 CLI 도구화 | `exec` 으로 우회만 가능 |

**영향도:** 중 — 생태계/확장성의 핵심

### GAP-7: Runtime 인게임 AI

| 항목 | IvanMurzak | 나머지 전부 |
|------|-----------|------------|
| 빌드된 게임 내 AI 실행 | 지원 (NPC 제어, 디버깅) | 미지원 |

**영향도:** 낮~중 — 특화된 사용 사례이지만 유니크한 차별점

### GAP-8: ProBuilder 메시 편집

| 항목 | CoplayDev v9.4.8 | unityctl |
|------|-------------------|----------|
| ProBuilder 통합 | `manage_probuilder` | 없음 (`mesh-create-primitive` 기본 도형만) |

**영향도:** 낮 — 프로토타이핑/블록아웃 워크플로우에 유용

---

## 3. 개선 로드맵

### Phase A: 즉시 실행 (코드 변경 최소 — 1~2주)

> 목표: 가시성 확보 + 배포 채널 확대

| ID | 작업 | 산출물 | 난이도 |
|----|------|--------|--------|
| A-1 | MCP 서버 레지스트리 등록 | mcpservers.org, awesome-mcp-servers 등록 | 낮음 |
| A-2 | Claude Code 마켓플레이스 플러그인 생성 | 마켓플레이스 스킬/플러그인 패키지 | 낮음 |
| A-3 | Unity Asset Store 등록 준비 | Plugin UPM 패키지 Asset Store 제출 | 중간 |
| A-4 | README 영문 강화 | GIF 데모, 비교표, 원클릭 설치 가이드 | 낮음 |
| A-5 | 경쟁 비교 문서 업데이트 | `competitive-claims-audit.md` 갱신 (검증된 claim만) | 낮음 |

### Phase B: 단기 기능 확장 (2~4주)

> 목표: 경쟁자 대비 핵심 갭 해소

| ID | 작업 | 산출물 | 난이도 | 명령 수 |
|----|------|--------|--------|---------|
| B-1 | **exec 강화** | 멀티라인, 체이닝, 인스턴스 메서드 지원 | 중간 | 기존 exec 확장 |
| B-2 | **reflect 명령** | `reflect find-types`, `reflect get-members`, `reflect call-method` | 중간 | +3 |
| B-3 | **Camera 제어** | `camera get`, `camera set` (FOV, clip, clear flags 등) | 중간 | +2 |
| B-4 | **Cinemachine 지원** | `cinemachine create`, `cinemachine configure` | 중간 | +2 |
| B-5 | **Unity Docs 조회** | `docs search` (ScriptReference + Manual 크롤/캐시) | 중간 | +1 |
| B-6 | **커스텀 도구 등록** | Plugin 측 `[UnityctlTool]` 어트리뷰트 → 자동 등록 | 높음 | 프레임워크 |

### Phase C: 렌더링 파이프라인 (3~5주)

> 목표: URP/HDRP 현대 프로젝트 완전 지원

| ID | 작업 | 산출물 | 난이도 | 명령 수 |
|----|------|--------|--------|---------|
| C-1 | **Volume Profile 관리** | `volume create`, `volume get`, `volume set`, `volume list` | 중간 | +4 |
| C-2 | **Post-Processing 설정** | `post-processing get`, `post-processing set` | 중간 | +2 |
| C-3 | **Renderer Features** | `renderer-feature list`, `renderer-feature add`, `renderer-feature remove` | 높음 | +3 |
| C-4 | **Rendering Stats 쿼리** | `rendering-stats get` | 낮음 | +1 |

### Phase D: 생태계 & 고급 기능 (5주+)

> 목표: 장기 차별화 + 생태계 구축

| ID | 작업 | 산출물 | 난이도 |
|----|------|--------|--------|
| D-1 | **ProBuilder 통합** | `probuilder create`, `probuilder edit-face/edge/vertex` | 높음 |
| D-2 | **Runtime Agent SDK** | PlayMode 양방향 통신, NPC 제어 API | 매우 높음 |
| D-3 | **Workflow 번들 도구** | `workflow compile-fix`, `workflow ui-smoke`, `workflow build-verify` | 중간 |
| D-4 | **Visual Verification v2** | Before/after screenshot diff, UI assertion helper | 중간 |
| D-5 | **멀티 인스턴스 라우팅** | `editor current/select`, session별 editor pin | 중간 |

---

## 4. 우선순위 매트릭스

```
영향도 높음 + 난이도 낮음 (즉시 실행):
  A-1 MCP 레지스트리 등록
  A-2 Claude Code 마켓플레이스
  A-4 README 영문 강화

영향도 높음 + 난이도 중간 (최우선 개발):
  B-1 exec 강화
  B-2 reflect 명령
  B-3 Camera 제어
  C-1 Volume Profile

영향도 중간 + 난이도 중간 (순차 개발):
  B-4 Cinemachine
  B-5 Unity Docs 조회
  B-6 커스텀 도구 등록
  C-2 Post-Processing

영향도 낮음 + 난이도 높음 (후순위):
  D-1 ProBuilder
  D-2 Runtime Agent
```

---

## 5. Unity 공식 도구 대응 전략

Unity `com.unity.ai.assistant`는 **Unity 6+ 전용, Closed-source, 가격 미정**입니다.

unityctl 방어 전략:
1. **LTS 호환성 유지** — Unity 2021.3/2022.3 사용자는 공식 도구 사용 불가
2. **오픈소스 + MIT** — 커스터마이징, 감사(audit), 자체 호스팅 가능
3. **Headless CI/CD** — 공식 도구는 Editor 필수, unityctl은 batchmode 지원
4. **토큰 효율성** — 12 통합 도구 vs 공식 도구의 동적 도구 목록
5. **Flight Recorder + Doctor** — 운영 가시성은 공식 도구에 없는 영역

---

## 6. 성공 지표 (KPI)

| 지표 | 현재 | 3개월 목표 | 6개월 목표 |
|------|------|-----------|-----------|
| GitHub Stars | - | 100+ | 500+ |
| MCP 레지스트리 등록 | 0 | 2+ (mcpservers.org, awesome-mcp) | 5+ |
| CLI 명령 수 | 118 | 130+ | 145+ |
| MCP 도구 수 | 12 (통합형) | 12~14 | 14~16 |
| dotnet 테스트 | 624 | 680+ | 750+ |
| Unity 실측 검증 명령 | 118 | 130+ | 145+ |

---

## 7. 참고 자료

### 경쟁사 소스

- [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) — 시장 1위, 7K stars
- [IvanMurzak/Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) — 52 tools, 런타임 지원
- [CoderGamester/mcp-unity](https://github.com/CoderGamester/mcp-unity) — Node.js 기반
- [yucchiy/UniCli](https://github.com/yucchiy/UniCli) — Go CLI, Claude Code 플러그인
- [youngwoocho02/unity-cli](https://github.com/youngwoocho02/unity-cli) — 단일 바이너리, 커스텀 도구
- [Unity AI Gateway Beta](https://create.unity.com/UnityAIGatewayBeta) — 공식 MCP

### 내부 소스

- `docs/internal/benchmark/competitive-claims-audit.md` — 기존 비교 검증
- `docs/ref/phase-roadmap.md` — 기존 로드맵 (다음 개발 로드맵 섹션)
- `docs/status/PROJECT-STATUS.md` — 현재 프로젝트 상태
