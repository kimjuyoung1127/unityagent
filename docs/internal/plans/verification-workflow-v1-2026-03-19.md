# Verification Workflow V1 구현 계획

> 작성일: 2026-03-19
> 근거: 실제 코드/문서 탐색, Unity 공식 문서 검토, 서브에이전트 2개 분석, Unity CLI 실측
> 상태: implementation-ready plan

## 요약

- 제품 방향은 하이브리드로 고정한다. 내부에는 Core 전용 verification runner를 추가하고, 외부 표면은 `workflow verify` CLI와 `unityctl_verify` MCP를 동시에 제공한다.
- v1 범위는 Edit 검증, Play Mode smoke, Play 시각 diff까지 포함한다.
- v1는 `실행 중인 Unity Editor + IPC ready`를 기본 전제로 한다. 현재 샘플 프로젝트 실측상 닫힌 Editor 상태의 `status`/`screenshot` batch fallback은 검증 용도로 신뢰하지 않는다.
- real click/key emulation은 v1 범위에서 제외하고 state/pixel/evidence 검증에 집중한다.

## 현재 확인된 사실

- `WorkflowCommand`는 현재 JSON 기반 순차 실행만 담당하며 evidence, assertion, artifact 개념이 없다.
- `ScreenshotHandler`는 현재 `SceneView`/`Camera.main` 기반 렌더 후 base64를 반환한다. Play end-of-frame 검증용 캡처 경로는 아직 없다.
- `UiGet`/`UiFind`는 읽기 데이터가 충분하고, `ui toggle`/`ui input`은 `SetIsOnWithoutNotify`/`SetTextWithoutNotify` 기반 deterministic setter다.
- `PlayModeHandler`는 상태 토글만 담당하며 settle/wait 로직이 없다.
- `WatchEventSource`는 `console`, `hierarchy`, `compilation` 스트림을 제공한다.
- 2026-03-19 실측 결과:
  - `editor list`는 Unity `6000.0.64f1` 설치를 확인했다.
  - `doctor --project tests/Unityctl.Integration/SampleUnityProject --json`는 `transport-degraded`를 반환했다.
  - 닫힌 Editor 상태의 `status`/`screenshot capture`는 `Unity exited with code 1 but no response file was written.`로 실패했다.

## 구현 방향

### 1. Shared/Core

- `VerificationDefinition`, `VerificationStep`, `VerificationResult`, `VerificationArtifact`를 추가한다.
- `VerificationStep.kind`는 v1에서 아래만 지원한다.
  - `projectValidate`
  - `uiAssert`
  - `sceneBaseline`
  - `sceneDiff`
  - `capture`
  - `imageDiff`
  - `consoleWatch`
  - `playSmoke`
- `VerificationResult`는 `passed`, `summary`, `steps`, `artifacts`, `timings`를 반환한다.
- 기본 응답은 artifact 경로, 해시, 메타데이터만 포함한다. base64 inline evidence는 opt-in으로만 허용한다.
- Core에 `VerificationRunner`를 추가해 기존 `status`, `project-validate`, `ui-get`, `scene snapshot/diff`, `watch`, `play-mode`를 조합한다.
- artifact 기본 저장 위치는 사용자 config 하위 `verification/<timestamp>/`로 고정한다. repo 내부에는 저장하지 않는다.

### 2. Plugin/Unity

- 기존 `screenshot capture` 의미는 유지한다.
- 새 Editor 명령 `verify-capture`를 추가하고 `mode=scene-camera|game-camera|game-end-of-frame`를 지원한다.
- `game-end-of-frame`는 Play Mode 전용으로 구현한다. Unity 공식 가이드대로 `ScreenCapture.CaptureScreenshotAsTexture`와 `WaitForEndOfFrame` 기반으로 캡처한다.
- Edit 모드의 기존 카메라 캡처는 현재 `Camera.Render` + `Texture2D.ReadPixels` 경로를 유지한다.
- 새 Editor 명령 `verify-image-diff`를 추가해 두 캡처를 비교하고 아래를 반환한다.
  - `changedPixelRatio`
  - `changedRegions`
  - `boundingBoxes`
  - `diffImagePath`
- `uiAssert`는 existing `ui-get` 결과와 레이아웃 안정화만으로 구현한다.
- Play smoke는 `play-mode` 이후 `status.data.isPlaying`을 기준으로 진입과 정착을 판단하고, watch window 동안 console/hierarchy를 수집한 뒤 end-of-frame 캡처와 diff를 수행한다.

### 3. CLI/MCP

- CLI 주 진입점은 아래로 고정한다.

```bash
unityctl workflow verify --file <definition.json> --project <path> [--artifacts-dir <path>] [--inline-evidence] [--json]
```

- MCP는 전용 `unityctl_verify` 도구를 추가하고 입력은 아래로 고정한다.
  - `project`
  - `definition`
  - `artifacts_dir?`
  - `include_inline_evidence?=false`
- 기존 generic `workflow run`은 유지하되 verification 전용 의미를 섞지 않는다.
- `JsonContext`, `CommandCatalog`, Plugin `CommandRegistry`, MCP annotation/blackbox 테스트를 모두 동기화한다.
- MCP top-level tool count는 12에서 13으로 갱신한다.

## 공식 문서 기준

- Play 시각 검증 캡처는 Unity 공식 `ScreenCapture.CaptureScreenshotAsTexture` + `WaitForEndOfFrame` 흐름을 기준으로 한다.
- Edit 모드 카메라 캡처는 현행 `Camera.Render` + `Texture2D.ReadPixels` 모델을 유지한다.
- UI verification은 현재 코드와 동일하게 `Toggle.SetIsOnWithoutNotify`, `InputField.SetTextWithoutNotify` 경계 위에서 설계한다.

## 테스트 계획

- Shared
  - verification 계약 serialization roundtrip
  - `JsonContext` 등록 검증
  - Shared/Plugin 복사본 동기화 검증
- CLI
  - `workflow verify` 파싱
  - JSON 출력
  - artifact 경로 정책
  - inline evidence opt-in
  - failure summary
- MCP
  - `unityctl_verify` 등록
  - annotation
  - black-box 호출
  - tool count 갱신
- Integration
  - 샘플 Unity 프로젝트 기준 `projectValidate` pass
  - edit-mode capture/diff pass
  - play enter/settle pass
  - play visual diff pass
  - forced console error fail
  - UI assert pass/fail
  - closed-editor structured failure

## Unity 실측 요구사항

- 구현 후 최소 2회 수동 검증을 포함한다.
  - `tests/Unityctl.Integration/SampleUnityProject`
  - 실제 실행 중 Editor 1개
- 확인 항목:
  - IPC ready 여부
  - Play 진입 안정화
  - end-of-frame 캡처 결과
  - diff artifact 생성
  - console evidence 누락 여부

## 자기 리뷰 체크리스트

- Shared 변경이 Plugin `Editor/Shared`에 정확히 복사됐는지 확인한다.
- docs가 preview 범위를 과장하지 않는지 확인한다.
- 기본 응답에 base64 evidence가 섞이지 않는지 확인한다.
- 기존 `screenshot capture`, `scene diff`, `workflow run`의 하위 호환성이 유지되는지 확인한다.

## 문서 업데이트 범위

- `docs/ref/phase-roadmap.md`
- `docs/status/PROJECT-STATUS.md`
- `docs/ref/ai-quickstart.md`

위 3개 문서는 실제 구현 턴에서 같은 변경 세트로 동기화하고, 실제 Unity 수동 검증이 끝나기 전까지는 preview로 표기한다.
