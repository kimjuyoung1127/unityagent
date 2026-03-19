# Phase 4B — Scene Diff

## 목표
Unity 씬 상태 스냅샷 캡처 + propertyPath 기반 diff.

## 선행 조건
- Phase 3C 완료

## 산출물

### Plugin 신규 (`Editor/Commands/`)
- `SceneSnapshotHandler.cs`:
  - `scene-snapshot` 커맨드 처리
  - `SceneManager.GetSceneAt()` 순회 (멀티씬)
  - 각 씬의 root GameObjects → 재귀 순회
  - 각 Component에 `new SerializedObject(comp)` → `GetIterator()` → `NextVisible(true)` 순회
  - `GlobalObjectId` 배치 API (`GetGlobalObjectIdsSlow(Object[], GlobalObjectId[])`)
  - 결과를 JSON (JObject) 반환
- `SceneDiffHandler.cs`:
  - `scene-diff` 커맨드 처리
  - 두 스냅샷 JSON 비교
  - propertyPath 키 기반 딕셔너리 diff
  - float epsilon 비교 (`1e-6`)
  - ADDED / REMOVED / CHANGED 분류

### Plugin 모델 (`Editor/Shared/` 또는 별도)
- `SceneSnapshot` 구조:
  - timestamp (ISO 8601), unityVersion
  - sceneSetup[]: path, isLoaded, isActive
  - scenes[]: path, name, isDirty, gameObjects[]
  - gameObjects[]: globalObjectId, name, activeSelf, layer, tag, scenePath, components[]
  - components[]: globalObjectId, typeName, enabled, properties (Dictionary<propertyPath, value>)
- `SceneDiffResult` 구조:
  - scenePath, addedObjects[], removedObjects[], modifiedObjects[]
  - modifiedObjects[]: globalObjectId, name, addedComponents[], removedComponents[], modifiedComponents[]
  - modifiedComponents[]: typeName, propertyChanges[] (propertyPath, oldValue, newValue, type)

### Core 수정 (최소)
- `CommandExecutor`는 기존 경로로 처리 (신규 transport 불필요)

### CLI 수정
- `SceneCommand.cs` 신규:
  - `unityctl scene snapshot --project <path> [--json]`
  - `unityctl scene diff <snap1.json> <snap2.json> [--json]`
  - `unityctl scene diff --live --project <path>` (현재 vs 마지막 스냅샷)
- 스냅샷 저장: `~/.unityctl/snapshots/{project-hash}/snap-{timestamp}.json`

### Shared 수정
- `WellKnownCommands.cs`: `SceneSnapshot = "scene-snapshot"`, `SceneDiff = "scene-diff"`
- `CommandCatalog.cs`: scene 커맨드 메타데이터

### 테스트
- `Shared.Tests/`: SceneSnapshot/SceneDiffResult 직렬화 round-trip
- `Cli.Tests/`: scene 커맨드 파라미터 파싱
- (실제 씬 스냅샷은 Unity Editor 필요 → 수동 검증)

### 성능 고려
- `NextVisible()` 사용 (프로퍼티 30~50% 감소)
- 배치 GlobalObjectId
- `scene.isDirty` 필터 (선택적 스냅샷)
- 대규모 씬 (100K+ GO): 씬/경로 필터 제공

## 규칙
- `docs/ref/code-patterns.md` 패턴 준수
- `TreatWarningsAsErrors=true`
- Plugin 코드는 Unity API 의존 → `dotnet build`로 컴파일 불가
- YAML 파싱은 미래 과제 (Phase 4B에서는 SerializedObject API만)
