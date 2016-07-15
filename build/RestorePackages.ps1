$path = $(Split-Path -parent $MyInvocation.MyCommand.path)

gci $path\..\ -Recurse "packages.config" |% {
	"Restoring " + $_.FullName
	& $path\..\src\.nuget\nuget.exe install $_.FullName -o $path\..\src\packages
}