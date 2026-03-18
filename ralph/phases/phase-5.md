# Phase 5 — Agent Layer

## 목표
AI 에이전트가 unityctl을 도구로 활용하기 위한 외부 인터페이스.
네이티브 .NET MCP 서버 + 스키마 자동 발견 + C# 식 실행.

## 선행 조건
- Phase 4B 완료 (모든 커맨드가 안정)

## 산출물

### P0 — Schema Command (필수, 가장 먼저)
- `ToolsCommand.cs` 확장 또는 `SchemaCommand.cs` 신규:
  - `unityctl schema --format json`
  - CommandCatalog 기반 전체 커맨드 스키마 출력
  - 각 커맨드별: name, description, parameters[] (name, type, required, description)
  - JSON Schema 호환 형식
- `CommandCatalog.cs` 확장: 파라미터 타입/required/description 메타데이터 보강

### P1 — MCP Server (`Unityctl.Mcp` 프로젝트 신설)
- `src/Unityctl.Mcp/Unityctl.Mcp.csproj` (net10.0):
  - NuGet: `ModelContextProtocol` (C# SDK)
  - ProjectReference: `Unityctl.Core`
- `src/Unityctl.Mcp/Program.cs`:
  - `Host.CreateApplicationBuilder` + `AddMcpServer()` + `WithStdioServerTransport()`
  - `WithToolsFromAssembly()`
- `src/Unityctl.Mcp/Tools/`:
  - 각 기존 커맨드를 `[McpServerTool]` + `[Description]` 래핑
  - DI로 Core 서비스 (CommandExecutor, SessionManager 등) 주입
  - build, test, check, ping, status, editor list, scene snapshot/diff, log, watch
- Stdio transport (Claude Code, VS Code, Cursor 호환)
- dotnet tool NuGet 패키지로 배포 가능 구조

### P2 — `unityctl exec` (C# 식 실행)
- Plugin `ExecHandler.cs`:
  - `exec` 커맨드: C# 식 문자열 수신
  - Unity Editor 내에서 Roslyn 또는 리플렉션으로 평가
  - 결과를 JSON 반환
  - 보안: 파일시스템 접근/프로세스 실행 제한 고려
- CLI `ExecCommand.cs`:
  - `unityctl exec "EditorApplication.isPlaying = true"`
  - `unityctl exec --file script.cs`
- `WellKnownCommands.Exec = "exec"`

### P3 — Workflow Runner (선택, 낮은 우선순위)
- `unityctl workflow run <file.json>`
- 단순 순차 실행, continueOnError/timeout만 지원
- 복잡한 오케스트레이션은 에이전트 책임

### 만들지 않을 것
- 커스텀 오케스트레이션 엔진
- 복잡한 워크플로 DSL (루프, 조건, 변수)
- 에이전트별 플러그인 (MCP가 범용 통합점)
- 자연어 인터페이스
- 상태 관리 (호출당 stateless)

### 테스트
- `Mcp.Tests/`: MCP 도구 등록 검증 (호스트 빌더 테스트)
- `Shared.Tests/`: schema 출력 형식 검증
- `Cli.Tests/`: exec 파라미터 파싱
- `Integration.Tests/`: `unityctl schema --format json` exit code 0 + JSON 유효성

## 규칙
- `docs/ref/code-patterns.md` 패턴 준수
- `TreatWarningsAsErrors=true`
- MCP 서버는 별도 프로젝트 (Core 참조만, Cli 참조 안 함)
- exec 보안: 위험한 API 호출 제한 고려
