dotnet restore
dotnet build ./**/project.json -c release 

$scriptPath = Split-Path $MyInvocation.MyCommand.Path
Write-Output $scriptPath

. ./UpdateNuspecVersion.ps1 -preRelease alpha -major 0 -minor 8

#Invoke-Expression "$scriptPath\UpdateNuspecVersion.ps1" $argumentList  

$children = gci ./ -recurse *.nuspec
foreach( $child in $children)
{
	Write-Output $child.FullName
	nuget pack -Verbosity detailed -OutputDirectory D:\LocalNuget $child.FullName
}