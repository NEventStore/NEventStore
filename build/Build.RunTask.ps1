Param(
	[string]$task,
	[bool]$runPersistenceTests = $false)

if($task -eq $null) {
	$task = read-host "Enter Task"
}

$scriptPath = $(Split-Path -parent $MyInvocation.MyCommand.path)

. .\build\psake.ps1 -scriptPath $scriptPath -t $task -properties @{ runPersistenceTests=$runPersistenceTests }
