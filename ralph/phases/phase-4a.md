# Phase 4A — Ghost Mode (Preflight Validation)

## 목표
빌드/테스트 전 사전 검증 (`--dry-run`). 실제 빌드 없이 오류를 미리 감지.

## 선행 조건
- Phase 3A 완료

## 산출물

### Plugin 수정 (`Editor/Commands/`)
- `BuildHandler.cs` 수정:
  - `dryRun` 파라미터 감지 시 검증만 수행, `BuildPipeline.BuildPlayer()` 호출 안 함
  - 검증 로직을 별도 메서드로 추출 (기존 `ParseBuildTarget()` 재활용)
  - 결과: checks 배열 (category/check/passed/message/details)
- `PreflightResult.cs` 신규 (Plugin Shared~):
  - 3단계: error (차단), warning (경고), info (참고)

### Plugin 검증 항목

Error (차단):
- 빌드 타겟 문자열 유효성 (`ParseBuildTarget()`)
- 플랫폼 모듈 설치 (`BuildPipeline.IsBuildTargetSupported()`)
- 활성화된 씬 존재 (`EditorBuildSettings.scenes`)
- 씬 파일 디스크 존재 (`File.Exists`)
- 스크립트 컴파일 에러 (`EditorUtility.scriptCompilationFailed`)
- 출력 경로 쓰기 가능

Warning (경고):
- 활성 빌드 타겟 불일치 (`EditorUserBuildSettings.activeBuildTarget`)
- Android SDK/NDK 경로 미발견
- 현재 컴파일 중

Info (참고):
- 활성화된 씬 목록
- 스크립팅 백엔드 (Mono/IL2CPP)
- 스크립팅 define symbols
- PlayerSettings 요약

### CLI 수정
- `BuildCommand.cs`에 `--dry-run` 파라미터 추가
- `CheckCommand.cs`에 `--dry-run` 파라미터 추가 (선택)
- `ConsoleOutput.cs`: preflight 결과 출력 (error=Red, warning=Yellow, info=Cyan)
- `CommandCatalog.cs`: build에 dryRun 파라미터 추가

### Shared 수정
- `CommandCatalog.cs` 업데이트

### 테스트
- `Shared.Tests/`: preflight 결과 직렬화 round-trip
- `Cli.Tests/`: --dry-run 파라미터 전달 검증
- `Integration.Tests/`: CLI `build --dry-run` exit code 0 (Unity 없이도 파라미터 파싱까지)

## 규칙
- Plugin 코드는 Unity API 의존 → `dotnet build`로 컴파일 불가, 문법 검증만
- `docs/ref/code-patterns.md` 패턴 준수
- `TreatWarningsAsErrors=true`
