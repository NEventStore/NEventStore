$configurationdefault = "Release"
$artifacts = "../artifacts"

$configuration = Read-Host 'Configuration to build [default: Release] ?'
if ($configuration -eq '') {
    $configuration = $configurationdefault
}
$runtests = Read-Host 'Run Tests (y / n) [default:n] ?'

# Install gitversion tool
dotnet tool restore
$output = dotnet tool run dotnet-gitversion | out-string

# GitVersion
Write-Host $output
$version = $output | ConvertFrom-Json
$assemblyVersion = $version.AssemblySemver
$assemblyFileVersion = $version.AssemblySemver
#$assemblyInformationalVersion = ($version.SemVer + "." + $version.Sha)
$assemblyInformationalVersion = ($version.InformationalVersion)
$nugetVersion = $version.NuGetVersion
Write-Host $assemblyVersion
Write-Host $assemblyFileVersion
Write-Host $assemblyInformationalVersion
Write-Host $nugetVersion

# Display minimal restore information
dotnet restore ./src/NEventStore.Core.sln --verbosity m

# Build
Write-Host "Building: "$nugetversion
dotnet build ./src/NEventStore.Core.sln -c $configuration --no-restore

# Testing
if ($runtests -eq "y") {
    Write-Host "Executing Tests"
    dotnet test ./src/NEventStore.Core.sln -c $configuration --no-build
    Write-Host "Tests Execution Complated"
}

# NuGet packages
Write-Host "NuGet Packages creation"
dotnet pack ./src/NEventStore/NEventStore.Core.csproj -c $configuration --no-build -o $artifacts /p:PackageVersion=$nugetversion
dotnet pack ./src/NEventStore.PollingClient/NEventStore.PollingClient.csproj -c $configuration --no-build -o $artifacts /p:PackageVersion=$nugetversion
dotnet pack ./src/NEventStore.Serialization.Json/NEventStore.Serialization.Json.Core.csproj -c $configuration --no-build -o $artifacts /p:PackageVersion=$nugetversion
dotnet pack ./src/NEventStore.Serialization.Bson/NEventStore.Serialization.Bson.Core.csproj -c $configuration --no-build -o $artifacts /p:PackageVersion=$nugetversion