Findings



\[P1] requestId만으로는 테스트 실행을 안전하게 식별할 수 없습니다. 현재 구현은 콜백을 등록한 뒤 api.Execute(...)의 반환 GUID를 버리고 있습니다. TestHandler.cs#L41 TestHandler.cs#L43 Unity 공식 문서는 콜백이 등록한 TestRunnerApi 인스턴스와 무관하게 모든 test run에서 온다고 설명합니다. 이 상태에서 2C 플랜처럼 requestId별 registry만 두면 CLI 2회 실행이나 Test Runner UI 실행과 콜백이 섞여 잘못된 요청이 완료될 수 있습니다. 최소한 single-flight로 막거나, Unity run GUID와 상관관계를 명시해야 합니다.



\[P1] domain reload를 “timeout으로 정리”하는 수준이면 PlayMode 지원과 충돌합니다. test는 이미 play mode를 허용합니다. TestHandler.cs#L19 Unity 공식 문서는 등록한 콜백이 domain reload 후 유지되지 않으므로 재등록이 필요하다고 명시합니다. 플랜처럼 reload 시 registry 유실 후 CLI timeout으로 끝내면, 지원하는 모드가 정상 시나리오에서도 300초 대기 후 실패할 수 있습니다. --wait를 PlayMode에서 막거나, reload를 넘기는 completion handoff가 필요합니다.



\[P1] TestResultCollector 집계는 leaf-only 처리가 없으면 결과가 틀릴 가능성이 큽니다. 현재 collector는 모든 TestFinished에서 카운트를 올립니다. TestHandler.cs#L82 TestHandler.cs#L84 Unity 문서는 TestFinished가 test tree의 각 레벨마다 호출된다고 설명하고, 예제도 !result.HasChildren 필터를 둡니다. 2C에서 실제 pass/fail/skip/total을 반환하려면 leaf만 집계하거나 RunFinished의 최종 aggregate를 사용해야 합니다.



\[P2] 완료된 결과를 첫 조회에서 바로 제거하면 polling이 비멱등이 아니게 됩니다. 계획대로 TryRemove를 완료 응답 반환 직후 수행하면, 응답 전송 뒤 pipe I/O가 한번만 깨져도 클라이언트는 완료 결과를 영구히 잃습니다. 현재 IPC 클라이언트도 IOException을 일반 통신 오류로 처리합니다. IpcTransport.cs#L45 완료 결과는 TTL 동안 재조회 가능하게 남기거나, 별도 ack/delete 단계가 있어야 합니다.



\[P3] test-result를 public tool catalog에 넣으면 외부 surface와 실제 CLI가 어긋납니다. tools는 CommandCatalog.All을 그대로 노출합니다. ToolsCommand.cs#L39 그런데 CLI 등록은 test까지만 있고 test-result verb는 없습니다. Program.cs#L23 내부 polling 전용 명령이면 catalog에서 숨기거나, 반대로 공식 CLI/API surface로 승격시켜 문서화해야 합니다.



Open Questions / Assumptions



requestId는 response.data가 아니라 응답 루트에도 이미 복사됩니다. CommandRegistry.cs#L59 Batch/CLI는 response.requestId를 1순위로 읽는 쪽이 더 안전합니다.

플랜 배경의 maxServerInstances=1은 현재 코드와 다릅니다. 실제 상수는 4입니다. IpcServer.cs#L22 다만 listen loop가 한 번에 한 연결을 처리하는 구조라 “긴 연결을 잡고 있지 말자”는 결론 자체는 여전히 맞습니다. IpcServer.cs#L147 IpcServer.cs#L198

자동화 테스트 계획은 AsyncCommandRunner 핵심 경로를 거의 직접 검증하지 못합니다. 기존 black-box integration harness는 30초에서 프로세스를 끊습니다. CliIntegrationTests.cs#L151 wait 성공/timeout/cancel은 unit test로 따로 잡는 편이 안전합니다.

Unity callback 관련 근거는 공식 문서 두 곳으로 확인했습니다: TestRunnerApi, How to get test results.

