param(
    [switch]$AsJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\\..")

function Get-Utf8ByteCount {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    return [System.Text.Encoding]::UTF8.GetByteCount($Text)
}

function Invoke-UnityctlCli {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    Push-Location $repoRoot
    try {
        $output = & dotnet run --project src/Unityctl.Cli -- @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "unityctl CLI exited with code $LASTEXITCODE for args: $($Arguments -join ' ')"
        }

        return ($output | Out-String).Trim()
    }
    finally {
        Pop-Location
    }
}

$schemaJson = Invoke-UnityctlCli @("schema", "--format", "json")
$toolsJson = Invoke-UnityctlCli @("tools", "--json")

$schema = $schemaJson | ConvertFrom-Json
$tools = $toolsJson | ConvertFrom-Json

$mcpToolFiles = Get-ChildItem (Join-Path $repoRoot "src\\Unityctl.Mcp\\Tools") -Filter "*Tool.cs" -File |
    Sort-Object Name
$mcpToolNames = foreach ($toolFile in $mcpToolFiles) {
    Select-String -Path $toolFile.FullName -Pattern '\[McpServerTool\(Name = "([^"]+)"\)\]' |
        ForEach-Object { $_.Matches[0].Groups[1].Value }
}

$result = [ordered]@{
    measuredAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    cli = [ordered]@{
        commandCount = $schema.commands.Count
        schemaBytes = (Get-Utf8ByteCount $schemaJson)
        schemaTokenEstimate = [math]::Round((Get-Utf8ByteCount $schemaJson) / 4.0, 1)
        toolsBytes = (Get-Utf8ByteCount $toolsJson)
        toolsTokenEstimate = [math]::Round((Get-Utf8ByteCount $toolsJson) / 4.0, 1)
    }
    mcp = [ordered]@{
        topLevelToolClassCount = $mcpToolFiles.Count
        topLevelToolClassFiles = @($mcpToolFiles.Name)
        topLevelToolCount = @($mcpToolNames).Count
        topLevelToolNames = @($mcpToolNames)
    }
    notes = @(
        "schemaBytes measures 'unityctl schema --format json' output, not MCP tools/list output.",
        "toolsBytes measures 'unityctl tools --json' output.",
        "Token estimate uses a coarse bytes/4 heuristic for apples-to-apples comparisons."
    )
}

if ($AsJson) {
    $result | ConvertTo-Json -Depth 6
    exit 0
}

Write-Output "Measured at (UTC): $($result.measuredAtUtc)"
Write-Output "CLI command count: $($result.cli.commandCount)"
Write-Output "CLI schema bytes: $($result.cli.schemaBytes)"
Write-Output "CLI schema token estimate: $($result.cli.schemaTokenEstimate)"
Write-Output "CLI tools bytes: $($result.cli.toolsBytes)"
Write-Output "CLI tools token estimate: $($result.cli.toolsTokenEstimate)"
Write-Output "MCP top-level tool class count: $($result.mcp.topLevelToolClassCount)"
Write-Output "MCP top-level tool count: $($result.mcp.topLevelToolCount)"
Write-Output "MCP top-level tool names:"
$result.mcp.topLevelToolNames | ForEach-Object { Write-Output " - $_" }
Write-Output "MCP top-level tool class files:"
$result.mcp.topLevelToolClassFiles | ForEach-Object { Write-Output " - $_" }
