[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$platformRelease = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../platform/build/Invoke-NuvIoTRelease.ps1'))
if (-not (Test-Path -LiteralPath $platformRelease -PathType Leaf)) {
    throw "V1 shared release entrypoint not found at expected sibling path: $platformRelease"
}

Write-Warning 'Publish-GitHubPackages.ps1 is a compatibility shim. Canonical publication runs through build-server run_release.'
& $platformRelease -SourceRoot $PSScriptRoot -Version $Version
if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
