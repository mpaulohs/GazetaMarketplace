#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Configure MCP servers for Claude Code
.DESCRIPTION
    Merges .docgen/mcp-config.json into Claude Code's configuration file.
    Detects platform (Windows/Linux) and locates appropriate config path.
.PARAMETER Rollback
    Restore the most recent backup of Claude Code configuration
.EXAMPLE
    pwsh .docgen/setup-mcp.ps1
.EXAMPLE
    pwsh .docgen/setup-mcp.ps1 -Rollback
#>

param(
    [switch]$Rollback
)

# Detect platform and config path
$configPath = if ($IsWindows -or $env:OS -eq "Windows_NT") {
    "$env:APPDATA\Claude\config.json"
} else {
    "$HOME/.config/claude/config.json"
}

Write-Host "Claude Code config path: $configPath"

# Check if config directory exists
$configDir = Split-Path $configPath -Parent
if (-not (Test-Path $configDir)) {
    Write-Host "Creating Claude Code config directory: $configDir"
    New-Item -ItemType Directory -Path $configDir -Force | Out-Null
}

# Backup existing config
if (Test-Path $configPath) {
    $backupPath = "$configPath.backup.$(Get-Date -Format 'yyyyMMddHHmmss')"
    Copy-Item $configPath $backupPath
    Write-Host "Backed up existing config to: $backupPath"
} else {
    Write-Host "No existing config found, will create new one"
}

if ($Rollback) {
    $latestBackup = Get-ChildItem "$configPath.backup.*" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($latestBackup) {
        Copy-Item $latestBackup.FullName $configPath -Force
        Write-Host " Restored config from: $($latestBackup.FullName)"
    } else {
        Write-Error " No backup found to restore"
        exit 1
    }
    exit 0
}

# Load configs
$claudeConfig = if (Test-Path $configPath) {
    Get-Content $configPath -Raw | ConvertFrom-Json
} else {
    [PSCustomObject]@{}
}

$mcpConfigPath = Join-Path (Get-Location) ".docgen/mcp-config.json"
if (-not (Test-Path $mcpConfigPath)) {
    Write-Error " MCP config not found: $mcpConfigPath"
    exit 1
}

$mcpConfig = Get-Content $mcpConfigPath -Raw | ConvertFrom-Json

# Ensure mcpServers property exists
if (-not $claudeConfig.PSObject.Properties['mcpServers']) {
    $claudeConfig | Add-Member -MemberType NoteProperty -Name "mcpServers" -Value ([PSCustomObject]@{})
}

# Merge MCP servers
foreach ($server in $mcpConfig.mcpServers.PSObject.Properties) {
    $serverName = $server.Name
    $serverConfig = $server.Value

    Write-Host "Adding MCP server: $serverName"

    if ($claudeConfig.mcpServers.PSObject.Properties[$serverName]) {
        # Update existing server
        $claudeConfig.mcpServers.PSObject.Properties.Remove($serverName)
    }

    $claudeConfig.mcpServers | Add-Member -MemberType NoteProperty -Name $serverName -Value $serverConfig -Force
}

# Save merged config
$claudeConfig | ConvertTo-Json -Depth 10 | Set-Content $configPath -Encoding UTF8

Write-Host ""
Write-Host " MCP servers configured successfully!"
Write-Host ""
Write-Host "Configured servers:"
foreach ($server in $mcpConfig.mcpServers.PSObject.Properties) {
    $enabled = if ($server.Value.enabled) { "" } else { "Ë" }
    Write-Host "  $enabled $($server.Name): $($server.Value.description)"
}
Write-Host ""
Write-Host "  Restart Claude Code to apply changes."
