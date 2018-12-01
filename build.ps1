$configurationdefault = "Release"
$artifacts = "../../artifacts"

$configuration = Read-Host 'Configuration to build [default: Release] ?'
if ($configuration -eq '') {
    $configuration = $configurationdefault
}
$runtests = Read-Host 'Run Tests (y / n) [default:n] ?'

# consider using NuGet to download the package (GitVersion.CommandLine)
choco install gitversion.portable --pre --y
choco upgrade gitversion.portable --pre --y

# Display .NET Core version
#dotnet --version

# Display minimal restore text
dotnet restore ./src/NEventStore.Core.sln --verbosity m

#gitversion 
$str = gitversion /updateAssemblyInfo | out-string
$json = convertFrom-json $str
$nugetversion = $json.NuGetVersion

Write-Host "Building: "$nugetversion
dotnet build ./src/NEventStore.Core.sln -c $configuration --no-restore

if ($runtests -eq "y") {
    Write-Host "Executing Tests"
    dotnet test ./src/NEventStore.Core.sln -c $configuration --no-build
}

Write-Host "NuGet Packages creation"
dotnet pack ./src/NEventStore/NEventStore.Core.csproj -c $configuration --no-build -o $artifacts /p:PackageVersion=$nugetversion
dotnet pack ./src/NEventStore.Serialization.Json/NEventStore.Serialization.Json.Core.csproj -c $configuration --no-build -o $artifacts /p:PackageVersion=$nugetversion