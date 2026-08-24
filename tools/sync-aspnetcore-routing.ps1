#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Vendors the ASP.NET Core routing sources that Scarlet.BlazorRouter compiles into itself.

.DESCRIPTION
    Scarlet.BlazorRouter cannot reference the Microsoft.AspNetCore.Routing package: the last
    standalone shipping of it is 2.3.0, which drags the whole ASP.NET Core 2.x package graph into
    Blazor WebAssembly, MAUI, WPF and WinForms hosts that have no use for it. From 3.0 onward
    routing exists only inside the Microsoft.AspNetCore.App shared framework, which those hosts do
    not have either.

    Microsoft solves the same problem the same way: Microsoft.AspNetCore.Components.dll compiles the
    routing sources directly, as internal types, guarded by the COMPONENTS define. See
    src/Components/Components/src/Microsoft.AspNetCore.Components.Routing.targets upstream. This
    script mirrors that file list into src/Scarlet.BlazorRouter/Routing/.

    Copies are byte-identical to upstream, so refreshing is a plain re-download and the checksum
    file detects any local edit.

.PARAMETER Tag
    dotnet/aspnetcore git tag to vendor from. Defaults to the pinned tag below.

.PARAMETER Verify
    Do not write anything. Re-hash the local files and fail if they differ from the recorded
    checksums. Intended for CI.

.EXAMPLE
    ./tools/sync-aspnetcore-routing.ps1
    ./tools/sync-aspnetcore-routing.ps1 -Tag v10.0.7
    ./tools/sync-aspnetcore-routing.ps1 -Verify
#>
[CmdletBinding()]
param(
    [string] $Tag = 'v10.0.6',
    [switch] $Verify
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$toolsDir = $PSScriptRoot
$repoRoot = Split-Path -Parent $toolsDir
$destRoot = Join-Path $repoRoot 'src/Scarlet.BlazorRouter/Routing'
$manifestPath = Join-Path $toolsDir 'aspnetcore-routing.files.txt'
$checksumPath = Join-Path $toolsDir 'aspnetcore-routing.sha256'

if (-not (Test-Path $manifestPath)) {
    throw "Manifest not found: $manifestPath"
}

$entries = foreach ($line in Get-Content $manifestPath) {
    $trimmed = $line.Trim()
    if ($trimmed.Length -eq 0 -or $trimmed.StartsWith('#')) { continue }

    $parts = $trimmed -split '\|', 2
    if ($parts.Count -ne 2) {
        throw "Malformed manifest line (expected '<upstream> | <local>'): $line"
    }

    [pscustomobject]@{
        Upstream = $parts[0].Trim()
        Local    = $parts[1].Trim()
    }
}

function Get-FileSha256 {
    param([string] $Path)
    (Get-FileHash -Path $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

if ($Verify) {
    if (-not (Test-Path $checksumPath)) {
        throw "Checksum file not found: $checksumPath. Run this script without -Verify first."
    }

    $expected = @{}
    foreach ($line in Get-Content $checksumPath) {
        $trimmed = $line.Trim()
        if ($trimmed.Length -eq 0 -or $trimmed.StartsWith('#')) { continue }
        $parts = $trimmed -split '\s+', 2
        $expected[$parts[1]] = $parts[0]
    }

    $problems = New-Object System.Collections.Generic.List[string]
    foreach ($entry in $entries) {
        $path = Join-Path $destRoot $entry.Local
        if (-not (Test-Path $path)) {
            $problems.Add("missing: $($entry.Local)")
            continue
        }
        if (-not $expected.ContainsKey($entry.Local)) {
            $problems.Add("not in checksum file: $($entry.Local)")
            continue
        }
        $actual = Get-FileSha256 $path
        if ($actual -ne $expected[$entry.Local]) {
            $problems.Add("modified: $($entry.Local)")
        }
    }

    if ($problems.Count -gt 0) {
        Write-Output "Vendored routing sources drifted from upstream:"
        foreach ($problem in $problems) { Write-Output "  $problem" }
        Write-Output ''
        Write-Output 'These files are copied verbatim from dotnet/aspnetcore. Re-run tools/sync-aspnetcore-routing.ps1 instead of editing them.'
        exit 1
    }

    Write-Output "OK: $($entries.Count) vendored files match $checksumPath."
    exit 0
}

$baseUrl = "https://raw.githubusercontent.com/dotnet/aspnetcore/$Tag"
Write-Output "Vendoring $($entries.Count) files from dotnet/aspnetcore@$Tag into $destRoot"

$checksumLines = New-Object System.Collections.Generic.List[string]
$checksumLines.Add("# sha256 of the files vendored by tools/sync-aspnetcore-routing.ps1")
$checksumLines.Add("# source: https://github.com/dotnet/aspnetcore/tree/$Tag")
$checksumLines.Add("# regenerate: ./tools/sync-aspnetcore-routing.ps1 -Tag $Tag")
$checksumLines.Add("")

foreach ($entry in $entries) {
    $destination = Join-Path $destRoot $entry.Local
    $destinationDir = Split-Path -Parent $destination
    if (-not (Test-Path $destinationDir)) {
        New-Item -ItemType Directory -Path $destinationDir -Force | Out-Null
    }

    $url = "$baseUrl/$($entry.Upstream)"
    try {
        Invoke-WebRequest -Uri $url -OutFile $destination -UseBasicParsing
    }
    catch {
        throw "Failed to download $url : $_"
    }

    $checksumLines.Add("$(Get-FileSha256 $destination)  $($entry.Local)")
    Write-Verbose "  $($entry.Local)"
}

Set-Content -Path $checksumPath -Value $checksumLines -Encoding UTF8
Write-Output "Wrote $checksumPath"
Write-Output "Done. Remember to bump the default -Tag in this script when moving to a new ASP.NET Core patch."
