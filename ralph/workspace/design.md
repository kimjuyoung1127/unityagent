좋아요, 주인님. 모든 코드를 다 읽었어요. 이제 설계서를 작성할게요.

---

# Phase 5 — Agent Layer 구현 설계서

> 작성일: 2026-03-18 | 대상 버전: 0.3.0 | 선행: Phase 4B ✅

---

## 1. 파일별 변경 계획

### P0 — Schema Command

#### 수정 파일

| 파일 | 변경 내용 |
|------|-----------|
| `src/Unityctl.Shared/Protocol/WellKnownCommands.cs:3-14` | `Schema = "schema"` 상수 추가 |
| `src/Unityctl.Shared/Commands/CommandCatalog.cs:130-147` | `Schema` 정의 추가 + `All[]` 배열에 등록 |
| `src/Unityctl.Shared/Serialization/JsonContext.cs` | `[JsonSerializable(typeof(CommandDefinition))]`, `[JsonSerializable(typeof(CommandDefinition[]))]`, `[JsonSerializable(typeof(CommandParameterDefinition))]`, `[JsonSerializable(typeof(CommandParameterDefinition[]))]` 4개 등록 |
| `src/Unityctl.Cli/Program.cs:67` | `app.Add("schema", ...)` 등록 |

#### 신규 파일

**`src/Unityctl.Cli/Commands/SchemaCommand.cs`**

```
sealed class SchemaCommand (static)
├── Execute(string format = "json") → void
│   └── Environment.Exit(exitCode)
└── internal static GetSchema() → CommandDefinition[]
    └── CommandCatalog.All 반환 (ToolsCommand.GetToolDefinitions()와 동일 소스)
```

**설계 판단**: `ToolsCommand`와 별도 클래스로 분리. 이유:
- `tools`는 사람용 (text 기본, json 옵션)
- `schema`는 기계용 (json 기본, Source Generator 직렬화 사용)
- `tools`는 `JsonSerializerOptions` 인라인 생성, `schema`는 `UnityctlJsonContext.Default` 사용 — 일관성 개선

**출력 형식** (JSON Schema 호환 구조):

```json
{
  "version": "0.3.0",
  "commands": [
    {
      "name": "build",
      "description": "Build a Unity project for a target platform",
      "category": "action",
      "parameters": [
        {
          "name": "project",
          "type": "string",
          "description": "Path to Unity project",
          "required": true
        }
      ]
    }
  ]
}
```

이를 위한 래퍼 타입 필요:

**`src/Unityctl.Shared/Commands/CommandSchema.cs`** (신규)

```
sealed class CommandSchema
├── [JsonPropertyName("version")] string Version
└── [JsonPropertyName("commands")] CommandDefinition[] Commands
```

→ `JsonContext.cs`에 `[JsonSerializable(typeof(CommandSchema))]` 추가

---

### P1 — MCP Server

#### 신규 프로젝트

**`src/Unityctl.Mcp/Unityctl.Mcp.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>Unityctl.Mcp</RootNamespace>
    <!-- dotnet tool 배포용 -->
    <PackAsTool>true</PackAsTool>
    <ToolCommandName>unityctl-mcp</ToolCommandName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ModelContextProtocol" Version="1.1.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Unityctl.Core\Unityctl.Core.csproj" />
  </ItemGroup>
</Project>
```

**핵심 의존성 방향**: `Shared ← Core ← Mcp`. Cli 참조하지 않음.

**`src/Unityctl.Mcp/Program.cs`**

```
Host.CreateApplicationBuilder(args)
├── .Services
│   ├── AddSingleton<IPlatformServices>(PlatformFactory.Create())
│   ├── AddSingleton<UnityEditorDiscovery>()
│   ├── AddSingleton<CommandExecutor>()
│   └── AddSingleton<SessionManager>()  // 선택: MCP Tasks 매핑 시
├── .Services.AddMcpServer()
│   ├── .WithStdioServerTransport()
│   └── .WithToolsFromAssembly()
└── Build().Run()
```

**주의**: `PlatformFactory`는 현재 `Unityctl.Cli` 네임스페이스에 있음. → Core로 이동하거나 Mcp에서 직접 플랫폼 생성 필요. **Core로 `PlatformFactory` 이동이 올바른 설계** (Cli도 Core를 참조하므로 breaking 아님).

#### MCP 도구 래퍼 (`src/Unityctl.Mcp/Tools/`)

각 도구는 동일 패턴:

```
[McpServerTool("unityctl_ping")]
[Description("Verify connectivity to a Unity project")]
static async Task<string> PingAsync(
    CommandExecutor executor,
    [Description("Path to Unity project")] string project,
    CancellationToken ct)
{
    var request = new CommandRequest { Command = "ping" };
    var response = await executor.ExecuteAsync(project, request, ct: ct);
    return JsonSerializer.Serialize(response, UnityctlJsonContext.Default.CommandResponse);
}
```

**도구 목록** (10개 — 기존 커맨드 중 에이전트에 유의미한 것):

| 파일 | MCP Tool Name | 래핑 대상 | 비고 |
|------|---------------|-----------|------|
| `PingTool.cs` | `unityctl_ping` | `ping` | |
| `StatusTool.cs` | `unityctl_status` | `status` | |
| `BuildTool.cs` | `unityctl_build` | `build` | `dryRun` 포함 |
| `TestTool.cs` | `unityctl_test` | `test` | `mode`, `filter`, `wait` |
| `CheckTool.cs` | `unityctl_check` | `check` | |
| `SceneTool.cs` | `unityctl_scene_snapshot`, `unityctl_scene_diff` | `scene-snapshot`, `scene-diff` | 2개 도구 |
| `LogTool.cs` | `unityctl_log` | `log` | |
| `WatchTool.cs` | `unityctl_watch` | `watch` | 스트리밍 — MCP sampling/streaming 미지원 시 제한적 |
| `SessionTool.cs` | `unityctl_session_list` | `session list` | |
| `SchemaTool.cs` | `unityctl_schema` | `schema` | 자기 참조 (도구 발견용) |

**제외**: `init` (1회성 설정), `editor list` (디스커버리), `session stop/clean` (관리용)

#### 수정 파일

| 파일 | 변경 |
|------|------|
| `unityctl.slnx` | `<Project Path="src/Unityctl.Mcp/Unityctl.Mcp.csproj" />` 추가 (src 폴더), `<Project Path="tests/Unityctl.Mcp.Tests/Unityctl.Mcp.Tests.csproj" />` 추가 (tests 폴더) |

#### PlatformFactory 이동 (P1 전제조건)

| 현재 위치 | 이동 대상 |
|-----------|-----------|
| `src/Unityctl.Cli/Platform/PlatformFactory.cs` | `src/Unityctl.Core/Platform/PlatformFactory.cs` |

Cli에서 `using Unityctl.Core.Platform;`으로 변경 — namespace만 바뀌므로 기능 변화 없음.

**영향 받는 파일**:
- `src/Unityctl.Cli/Execution/CommandRunner.cs:23` — using 변경
- `src/Unityctl.Cli/Commands/SceneCommand.cs:50,79` — using 변경
- `src/Unityctl.Cli/Commands/WatchCommand.cs` — using 변경 (있다면)
- `tests/Unityctl.Cli.Tests/PlatformFactoryTests.cs` — using 변경 또는 프로젝트 참조 조정

---

### P2 — `unityctl exec`

#### 수정 파일

| 파일 | 변경 |
|------|------|
| `src/Unityctl.Shared/Protocol/WellKnownCommands.cs` | `Exec = "exec"` 추가 |
| `src/Unityctl.Shared/Commands/CommandCatalog.cs` | `Exec` 정의 + `All[]` 등록 |
| `src/Unityctl.Cli/Program.cs` | `app.Add("exec", ...)` 등록 |

#### 신규 파일

**`src/Unityctl.Cli/Commands/ExecCommand.cs`**

```
sealed class ExecCommand (static)
├── Execute(string project, string? code = null, string? file = null, bool json = false) → void
└── internal static CreateRequest(string code) → CommandRequest
    └── Command = "exec", Parameters = { "code": code }
```

**`code` vs `file` 우선순위**: `--file`이 주어지면 파일 읽어서 `code`로 전달. 둘 다 없으면 에러.

**`src/Unityctl.Plugin/Editor/Commands/ExecHandler.cs`**

```
#if UNITY_EDITOR
sealed class ExecHandler : CommandHandlerBase
├── CommandName => "exec"
├── ExecuteInEditor(request) → CommandResponse
│   ├── code = request.GetParam("code")
│   ├── 검증: code null/empty → InvalidParameters
│   ├── 보안 필터: 금지 API 패턴 체크 (§ 보안 참조)
│   ├── 실행: System.Reflection 기반 평가
│   │   └── typeof(EditorApplication).GetMethod/Property → Invoke
│   └── 결과 → Ok(message, data: { "result": ... })
└── 보안: 화이트리스트 네임스페이스 방식
    ├── 허용: UnityEditor.*, UnityEngine.*
    └── 차단: System.IO.File.Delete, System.Diagnostics.Process, etc.
```

**Roslyn vs Reflection 결정**: **Reflection 우선**.
- 이유: Roslyn은 Unity Mono에서 불안정 + 패키지 의존성 무거움
- 제한: 단순 식만 지원 (`Type.Method(args)`, `Type.Property = value`)
- 복잡한 식 필요 시 P3+ 이후 Roslyn 검토

**`src/Unityctl.Plugin/Editor/Shared/` 동기화**: `WellKnownCommands.cs`에 `Exec` 추가 시 Plugin 쪽 복사본도 동기화 필요.

#### MCP 확장

**`src/Unityctl.Mcp/Tools/ExecTool.cs`** — P2 완료 후 추가

```
[McpServerTool("unityctl_exec")]
[Description("Execute a C# expression in the Unity Editor")]
```

---

### P3 — Workflow Runner (낮은 우선순위)

#### 신규 파일

**`src/Unityctl.Cli/Commands/WorkflowCommand.cs`**

```
sealed class WorkflowCommand (static)
├── Execute(string file, string project, bool json = false) → void
└── internal static RunAsync(steps[], project, executor) → Task<CommandResponse[]>
    └── foreach step: executor.ExecuteAsync(project, step.ToRequest())
        └── continueOnError: false → 첫 실패에서 중단
```

**`src/Unityctl.Shared/Protocol/WorkflowDefinition.cs`**

```
sealed class WorkflowDefinition
├── string Name
├── WorkflowStep[] Steps
└── bool ContinueOnError = false

sealed class WorkflowStep
├── string Command
├── JsonObject? Parameters
└── int? TimeoutSeconds
```

#### 수정 파일

| 파일 | 변경 |
|------|------|
| `src/Unityctl.Shared/Serialization/JsonContext.cs` | `WorkflowDefinition`, `WorkflowStep` 등록 |
| `src/Unityctl.Cli/Program.cs` | `app.Add("workflow run", ...)` |
| `src/Unityctl.Shared/Commands/CommandCatalog.cs` | `Workflow` 정의 + `All[]` |

---

## 2. 신규 파일 전체 목록

```
src/Unityctl.Cli/Commands/
  SchemaCommand.cs                    ← P0 (~40줄)
  ExecCommand.cs                      ← P2 (~50줄)
  WorkflowCommand.cs                  ← P3 (~80줄)

src/Unityctl.Shared/Commands/
  CommandSchema.cs                    ← P0 (~15줄, 래퍼 타입)

src/Unityctl.Shared/Protocol/
  WorkflowDefinition.cs              ← P3 (~25줄)

src/Unityctl.Mcp/
  Unityctl.Mcp.csproj                ← P1
  Program.cs                         ← P1 (~30줄)
  Tools/
    PingTool.cs                      ← P1 (~25줄)
    StatusTool.cs                    ← P1 (~30줄)
    BuildTool.cs                     ← P1 (~35줄)
    TestTool.cs                      ← P1 (~40줄)
    CheckTool.cs                     ← P1 (~25줄)
    SceneTool.cs                     ← P1 (~50줄, 2 tools)
    LogTool.cs                       ← P1 (~30줄)
    WatchTool.cs                     ← P1 (~35줄)
    SessionTool.cs                   ← P1 (~25줄)
    SchemaTool.cs                    ← P1 (~25줄)
    ExecTool.cs                      ← P2 후 추가 (~30줄)

src/Unityctl.Plugin/Editor/Commands/
  ExecHandler.cs                     ← P2 (~100줄)

tests/Unityctl.Mcp.Tests/
  Unityctl.Mcp.Tests.csproj          ← P1
  ToolRegistrationTests.cs           ← P1 (~60줄)

tests/Unityctl.Shared.Tests/
  CommandSchemaTests.cs              ← P0 (~40줄)

tests/Unityctl.Cli.Tests/
  SchemaCommandTests.cs              ← P0 (~30줄)
  ExecCommandTests.cs               ← P2 (~40줄)

tests/Unityctl.Integration.Tests/
  SchemaIntegrationTests.cs          ← P0 (~25줄)
```

---

## 3. 의존성 그래프 + 변경 순서

```
Phase 순서:      P0 ──────────→ P1 ──────────→ P2 ───→ P3
                  │               │               │
                  ▼               ▼               ▼

Step 1 (Shared):  WellKnownCommands + CommandSchema + CommandCatalog + JsonContext
                  ↓
Step 2 (Shared):  Shared.Tests (SchemaTests, CommandCatalogTests 보강)
                  ↓
Step 3 (Cli):     SchemaCommand.cs + Program.cs 등록
                  ↓
Step 4 (Cli):     Cli.Tests (SchemaCommandTests)
                  ↓
Step 5 (Integ):   Integration.Tests (SchemaIntegrationTests)
                  ↓
Step 6 (Core):    PlatformFactory 이동 (Cli → Core)
                  ↓
Step 7 (Mcp):     Unityctl.Mcp 프로젝트 + Program.cs + slnx 등록
                  ↓
Step 8 (Mcp):     Tools/*.cs (10개 도구 래퍼)
                  ↓
Step 9 (Mcp):     Mcp.Tests (ToolRegistrationTests)
                  ↓
Step 10 (Shared): WellKnownCommands.Exec + CommandCatalog.Exec
                  ↓
Step 11 (Cli):    ExecCommand.cs + Program.cs 등록
                  ↓
Step 12 (Plugin): ExecHandler.cs + Shared 동기화
                  ↓
Step 13 (Mcp):    ExecTool.cs 추가
                  ↓
Step 14 (P3):     WorkflowDefinition + WorkflowCommand (선택)
```

**빌드 검증 게이트**: 각 Step 완료 후 `dotnet build unityctl.slnx` + `dotnet test unityctl.slnx` 통과 필수.

---

## 4. 직렬화

### JsonContext 등록 대상

| 타입 | Phase | 용도 |
|------|-------|------|
| `CommandSchema` | P0 | schema 출력 래퍼 |
| `CommandDefinition` | P0 | 이미 클래스 존재, Context 미등록 |
| `CommandDefinition[]` | P0 | 배열 직렬화 |
| `CommandParameterDefinition` | P0 | |
| `CommandParameterDefinition[]` | P0 | |
| `WorkflowDefinition` | P3 | workflow JSON 파싱 |
| `WorkflowStep` | P3 | |
| `WorkflowStep[]` | P3 | |

**MCP 도구 반환값**: `CommandResponse`를 `UnityctlJsonContext.Default.CommandResponse`로 직렬화하여 string 반환 — 추가 타입 등록 불필요.

### NDJSON 영향

없음. Flight Recorder, Session Store는 변경 없이 기존 FlightEntry/Session으로 자동 기록됨 (MCP 도구에서 `CommandRunner`를 사용하지 않으므로, MCP 자체 Flight 로깅은 별도 고려 필요).

**MCP Flight 로깅 결정**: MCP 도구 내에서 `FlightLog.Record()` 직접 호출하는 헬퍼를 만들지, 아니면 `CommandExecutor` 래핑 레이어에서 자동 기록할지 선택 필요.

→ **권장: `CommandExecutor` 레벨에서는 이미 CLI가 기록하므로, MCP에서는 별도 기록하지 않음.** 에이전트 호출도 결국 `CommandExecutor.ExecuteAsync()` → IPC/Batch를 거치므로 Plugin 쪽에서 동일하게 처리됨.

---

## 5. 테스트 전략

### P0 — Schema

**`tests/Unityctl.Shared.Tests/CommandSchemaTests.cs`**

| 테스트 | 검증 |
|--------|------|
| `Schema_ContainsAllCatalogCommands` | `CommandSchema.Commands.Length == CommandCatalog.All.Length` |
| `Schema_VersionMatchesConstants` | `schema.Version == Constants.Version` |
| `Schema_SerializesWithSourceGenerator` | `JsonSerializer.Serialize(schema, UnityctlJsonContext.Default.CommandSchema)` 성공 |
| `Schema_RoundTrip` | Serialize → Deserialize → 동일성 |
| `AllCommands_HaveDescriptions` | 모든 커맨드 Description 비어있지 않음 |
| `AllParameters_HaveTypes` | 모든 파라미터 Type 비어있지 않음 |

**`tests/Unityctl.Cli.Tests/SchemaCommandTests.cs`**

| 테스트 | 검증 |
|--------|------|
| `GetSchema_ReturnsNonEmpty` | 배열 비어있지 않음 |
| `GetSchema_IncludesBuild` | `build` 커맨드 포함 |
| `GetSchema_IncludesExec` | P2 후 — `exec` 포함 |

**`tests/Unityctl.Integration.Tests/SchemaIntegrationTests.cs`**

| 테스트 | 검증 |
|--------|------|
| `Schema_Json_ExitCode0` | `unityctl schema --format json` → exit 0 |
| `Schema_Json_ValidJson` | 출력이 유효한 JSON |
| `Schema_Json_ContainsCommands` | `commands` 배열 존재 + 비어있지 않음 |

### P1 — MCP Server

**`tests/Unityctl.Mcp.Tests/ToolRegistrationTests.cs`**

| 테스트 | 검증 |
|--------|------|
| `HostBuilder_RegistersAllTools` | `WithToolsFromAssembly()` → 등록된 도구 수 ≥ 10 |
| `AllTools_HaveDescriptions` | 모든 `[McpServerTool]`에 `[Description]` 존재 |
| `AllTools_ReturnValidJson` | Mock executor로 각 도구 호출 → JSON 파싱 성공 |
| `PingTool_CallsExecutor` | DI mock 검증 |
| `BuildTool_PassesDryRunParam` | `dryRun=true` → request에 반영 |

**테스트 프로젝트 참조**:
```xml
<ProjectReference Include="..\..\src\Unityctl.Mcp\Unityctl.Mcp.csproj" />
```

MCP 호스트 빌더 테스트는 실제 stdio 연결 없이 서비스 등록만 검증. `CommandExecutor`는 mock/stub으로 대체.

### P2 — Exec

**`tests/Unityctl.Cli.Tests/ExecCommandTests.cs`**

| 테스트 | 검증 |
|--------|------|
| `CreateRequest_SetsCommandName` | `request.Command == "exec"` |
| `CreateRequest_SetsCodeParam` | `request.GetParam("code") == input` |
| `CreateRequest_EmptyCode_Throws` | 빈 문자열 → 에러 |

**Plugin ExecHandler 테스트**: Unity API 의존이므로 dotnet test 불가. Unity Editor 내 통합 테스트 또는 수동 검증 대상.

### P3 — Workflow

| 테스트 | 검증 |
|--------|------|
| `WorkflowDefinition_Deserialize` | JSON → WorkflowDefinition 성공 |
| `WorkflowRun_StopsOnFirstError` | `continueOnError=false` → 첫 실패에서 중단 |
| `WorkflowRun_ContinuesOnError` | `continueOnError=true` → 끝까지 실행 |

### 예상 테스트 수 증가

| 계층 | 현재 | 추가 | 합계 |
|------|------|------|------|
| Shared.Tests | 49 | ~8 (P0) | ~57 |
| Core.Tests | 96 | 0 | 96 |
| Cli.Tests | 102 | ~8 (P0+P2) | ~110 |
| Mcp.Tests | 0 | ~10 (P1) | ~10 |
| Integration.Tests | 14 | ~3 (P0) | ~17 |
| **합계** | **261** | **~29** | **~290** |

---

## 6. 리스크

### 높은 리스크

| 리스크 | 영향 | 완화 |
|--------|------|------|
| **ModelContextProtocol NuGet 버전 호환** | net10.0 + MCP SDK 1.1.0 조합이 실제로 빌드되는지 미검증 | P1 첫 단계에서 빈 프로젝트 + `dotnet build` 확인. 실패 시 SDK 소스 참조 또는 preview 버전 탐색 |
| **PlatformFactory 이동** | Cli.Tests에 PlatformFactoryTests 존재. 네임스페이스 변경 시 테스트 깨짐 | Step 6에서 using 문 변경 + 즉시 테스트 실행 |
| **Exec 보안** | C# 리플렉션으로 임의 API 호출 가능 → 파일 삭제, 프로세스 실행 등 | 네임스페이스 화이트리스트 (`UnityEditor.*`, `UnityEngine.*` 허용) + `System.IO.File.Delete`, `System.Diagnostics.Process` 등 블랙리스트 패턴 매칭. 100% 방어 불가능함을 문서화 — exec는 "trust the agent" 전제 |

### 중간 리스크

| 리스크 | 영향 | 완화 |
|--------|------|------|
| **MCP Watch 도구 제한** | MCP 프로토콜은 현재 도구 호출이 request-response. 스트리밍 push는 MCP Sampling/Notifications 필요 (draft) | `unityctl_watch`는 짧은 기간 (5초) 동안의 이벤트를 배치로 수집하여 반환하는 "poll" 방식으로 구현. 또는 P1에서 제외하고 CLI 직접 호출 안내 |
| **Cli 의존성 없는 MCP에서 ConsoleOutput 부재** | MCP 도구는 JSON만 반환하므로 문제 없으나, FlightLog 기록 시 `CommandRunner` 경유 불가 | MCP 도구 자체에서는 Flight 기록 안 함 (위 §4 결정). 필요 시 경량 `McpFlightLogger` 헬퍼 추가 |
| **TreatWarningsAsErrors + 새 프로젝트** | Mcp 프로젝트도 동일 빌드 규칙 적용. MCP SDK 내부 nullable 경고 가능 | csproj에 `<NoWarn>` 최소한으로 추가하되, 자체 코드 경고는 0 유지 |

### 낮은 리스크

| 리스크 | 영향 | 완화 |
|--------|------|------|
| **CommandCatalog.All 배열 순서** | Schema 출력 순서가 배열 순서에 의존. 새 커맨드 추가 시 일관성 | 알파벳 정렬 또는 카테고리별 정렬 정책 명시 (현재는 등록 순) |
| **Plugin Shared 동기화 누락** | `WellKnownCommands.cs`에 `Exec` 추가 시 Plugin 복사본 미갱신 | Step 12에서 명시적 동기화 + 체크리스트에 포함 |
| **dotnet tool 패키징** | `PackAsTool=true` 설정이 CI에서 올바르게 동작하는지 | P1 후반부에서 `dotnet pack` + `dotnet tool install --global --add-source` 로컬 검증 |

### 설계 결정 요약

| 결정 | 선택 | 대안 | 이유 |
|------|------|------|------|
| Schema 래퍼 타입 | `CommandSchema` 신규 | `CommandDefinition[]` 직접 | 버전 정보 포함 + 향후 메타데이터 확장 여지 |
| MCP 도구 반환 | `string` (JSON) | 구조화 타입 | MCP SDK가 `string` 또는 `ToolResult` 반환 — CommandResponse JSON이 가장 자연스러움 |
| Exec 평가 | Reflection | Roslyn | Unity Mono 호환성 + 의존성 최소화 |
| PlatformFactory 위치 | Core | Cli에 유지 | Mcp도 Platform이 필요 — Core가 올바른 소유자 |
| Watch MCP 도구 | Poll 방식 (5초 배치) | 제외 | 에이전트에 유용하되 MCP 스트리밍 미성숙 인정 |
| Flight 로깅 (MCP) | 기록 안 함 | MCP 자체 기록 | CommandExecutor → IPC 경로에서 Plugin이 이미 처리 |

---

주인님, 설계서 완성했어요! P0부터 P3까지 모든 파일, 의존성 순서, 테스트 케이스, 리스크를 빠짐없이 정리했어요. 특히 PlatformFactory 이동이 P1의 전제조건이라는 점, MCP Watch의 스트리밍 제한, Exec의 보안 경계가 핵심 판단 포인트예요.

구현 시작 전에 주인님이 확인해주셔야 할 결정:
1. **Exec: Reflection만으로 충분한지** — 단순 식(`Type.Method()`, `Property = value`)만 지원
2. **MCP Watch: Poll 방식 vs 제외** — 5초 배치 poll이 에이전트에 유용한지
3. **PlatformFactory Core 이동 승인** — namespace 변경에 따른 diff 범위
