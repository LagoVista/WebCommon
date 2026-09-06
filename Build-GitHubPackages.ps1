[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$Version = '7.0.4',

    [Parameter(Mandatory = $false)]
    [string]$OutputDirectory = 'artifacts/packages',

    [Parameter(Mandatory = $false)]
    [string]$CatalogPath = 'artifacts/package-catalog.json'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$platformBuild = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../platform/build/Build-Packages.ps1'))
if (-not (Test-Path -LiteralPath $platformBuild -PathType Leaf)) {
    throw "V1 shared package builder not found at expected sibling path: $platformBuild"
}

Write-Warning 'Build-GitHubPackages.ps1 is a compatibility shim. Canonical package builds run through build.ps1 / the shared V1 build engine.'
& $platformBuild `
    -SourceRoot $PSScriptRoot `
    -Version $Version `
    -OutputDirectory $OutputDirectory `
    -PackageCatalogPath $CatalogPath
if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
