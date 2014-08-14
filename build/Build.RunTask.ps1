Param(
	[string]$task,
	[string]$buildNumber = 0,
	[bool]$runPersistenceTests = $false)

if($task -eq $null) {
	$task = read-host "Enter Task"
}

$scriptPath = $(Split-Path -parent $MyInvocation.MyCommand.path)

$packageConfigs = Get-ChildItem src -Recurse | where{$_.Name -eq "packages.config"}
foreach($packageConfig in $packageConfigs){
	Write-Host "Restoring" $packageConfig.FullName
	src\.nuget\nuget.exe i $packageConfig.FullName -o src\packages
}

Import-Module .\src\packages\psake.4.3.2\tools\psake.psm1
Invoke-Psake .\build\default.ps1 -framework "4.0x64" -t $task -properties @{ build_number=$buildNumber;runPersistenceTests=$runPersistenceTests }
