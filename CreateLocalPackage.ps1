$scriptPath = Split-Path $MyInvocation.MyCommand.Path
Set-Location $scriptPath

dotnet restore
dotnet build $sciptpath/**/project.json -c release 
Write-Output $scriptPath

. ./UpdateNuspecVersion.ps1 -preRelease alpha -major 0 -minor 8

#Invoke-Expression "$scriptPath\UpdateNuspecVersion.ps1" $argumentList  

$children = gci ./ -recurse *.nuspec
foreach( $child in $children)
{
	Write-Output $child.FullName
	nuget pack -Verbosity detailed -OutputDirectory D:\LocalNuget $child.FullName
}