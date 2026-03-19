# Headless Batch Validation

최종 업데이트: 2026-03-18 (KST)

측정 기준:

- Tool: `C:\Users\ezen601\Desktop\Jason\unityctl\src\Unityctl.Cli\bin\Release\net10.0\unityctl.exe`
- Closed-editor project: `C:\Users\ezen601\Desktop\Jason\unityctl\tests\Unityctl.Integration\SampleUnityProject`
- 샘플 프로젝트는 repo에 포함되며 `com.unityctl.bridge` + `com.unity.test-framework`를 사용
- 순차 실행 기준으로 측정 (`status -> check -> test --mode edit -> build --dry-run`)

## 결과

| 시나리오 | 결과 | 근거 |
|----------|------|------|
| headless `status` | ✅ 성공 | `statusCode=0`, `message="Ready"` |
| headless `check` | ✅ 성공 | `statusCode=0`, `assemblies=2` |
| headless `test --mode edit` | ✅ 성공 | `statusCode=0`, `1 passed` |
| headless `build --dry-run` | ⚠️ structured failure | `statusCode=503`, `Preflight failed`, `OutputPath` not writable |

## 해석

- `unityctl` batch fallback은 repo에 포함된 closed-editor 샘플 프로젝트에서 `status`, `check`, `EditMode test`까지 재현 가능하게 검증되었다.
- `build --dry-run`은 crash가 아니라 structured preflight 결과까지 도달함이 확인되었다.
- 현재 샘플 프로젝트의 `dry-run` 실패 원인은 transport가 아니라 `OutputPath` 쓰기 불가 preflight error다.
- 따라서 현재 시점에서 README/프로젝트 상태 문서에는
  - "Editor 없이 meaningful CI-safe 작업 가능"
  - "샘플 프로젝트에서 status/check/test가 재현 가능"
  - "모든 headless 명령이 모든 프로젝트에서 성공"
  를 구분해서 써야 한다.

## CoplayDev 비교

| 시나리오 | CoplayDev |
|----------|-----------|
| Editor 없이 status/check/test/build | N/A / 실측 불가 |

이 저장소에서는 CoplayDev의 public editor-first quickstart만 확인했으며, 동일한 repo-contained closed-editor batch parity는 실측하지 않았다.

## 관련 산출물

- 자동 검증:
  - `tests/Unityctl.Integration.Tests/HeadlessBatchValidationTests.cs`
  - `tests/Unityctl.Integration/SampleUnityProject/`
