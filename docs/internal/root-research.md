멀티 인스턴스 라우팅
여러 Unity Editor를 동시에 다루는 선택/고정 기능이 필요합니다. 경쟁 MCP들은 unity_instances, set_active_instance를 전면에 둡니다. 지금 unityctl은 editor discovery는 강하지만, 에이전트가 “이 작업은 이 에디터에만” 고정하는 UX가 약합니다.
참고: CoplayDev/unity-mcp

읽기/탐색 API 확장
현재는 수정 API가 강한 반면, 에이전트가 안전하게 판단하기 위한 탐색 API가 부족합니다. 특히 hierarchy 조회, gameobject find, component 값 조회, find by component, reference graph, asset dependency 분석은 꼭 필요합니다. 이게 있어야 “수정 전에 상태 파악”이 가능해집니다.
참고: unity-editor-mcp

스크린샷/시각 피드백
Game View/Scene View 캡처, before/after 비교, UI 레이아웃 검증이 필요합니다. 경쟁 쪽은 이미 screenshot capture를 제공하고, Coplay는 시각 피드백을 핵심 차별점으로 말합니다.
참고: unity-editor-mcp Coplay 비교 글, 2025-10-10

배치 편집/트랜잭션
여러 조작을 한 번에 보내는 batch_execute 류가 필요합니다. 지금도 개별 커맨드는 충분히 많은데, 에이전트 완수율은 “호출 수 감소 + 부분 실패 롤백”에서 크게 올라갑니다.
참고: CoplayDev/unity-mcp

Graphics/Camera 영역
특히 URP/HDRP, Volume, renderer features, light baking, camera/Cinemachine은 아직 비어 있습니다. 경쟁 오픈소스도 최근 이쪽을 계속 확장 중이라, 실제 게임 프로젝트 대체력을 높이려면 중요합니다.
참고: CoplayDev/unity-mcp

고급 UI 자동화
지금은 UI 생성/Rect 조정까지는 있지만, UI 찾기, 상태 읽기, 클릭, 값 입력, 입력 시퀀스 재생이 없습니다. UI 제작과 테스트를 진짜 대체하려면 이 레이어가 필요합니다.
참고: unity-editor-mcp

전문화 도메인
texture import settings, shader/shader graph, scriptable object 관리, vfx, audio, terrain, probuilder가 남아 있습니다. 이건 범용성보단 특정 팀에서 체감이 큰 영역입니다.
참고: CoplayDev/unity-mcp

스크립트 편집 v2
v1은 이미 있으니 제외해야 하지만, 경쟁력 관점에서는 다음 단계가 필요합니다. whole-file replace를 넘어서 text edits, symbol-aware patch, find refs, diff preview, compile error -> 자동 수정 루프까지 가야 합니다. 지금 빠진 건 “스크립트 편집 자체”가 아니라 “안전한 부분 수정 UX”입니다.ScriptCommand.cs