$configurationdefault = "Release"
$artifacts = "../../artifacts"

$configuration = Read-Host 'Configuration to build [default: Release] ?'
if ($configuration -eq '') {
    $configuration = $configurationdefault
}
$runtests = Read-Host 'Run Tests (y / n) [default:n] ?'

# Install gitversion tool
dotnet tool restore

# Display minimal restore information
dotnet restore ./src/NEventStore.Core.sln --verbosity m

# GitVersion 
$str = dotnet tool run dotnet-gitversion /updateAssemblyInfo | out-string
$json = convertFrom-json $str
$nugetversion = $json.SemVer

# Build
Write-Host "Building: "$nugetversion
dotnet build ./src/NEventStore.Core.sln -c $configuration --no-restore -p:ContinuousIntegrationBuild=True

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
dotnet pack ./src/NEventStore.Serialization.MsgPack/NEventStore.Serialization.MsgPack.Core.csproj -c $configuration --no-build -o $artifacts /p:PackageVersion=$nugetversion
dotnet pack ./src/NEventStore.Serialization.Binary/NEventStore.Serialization.Binary.Core.csproj -c $configuration --no-build -o $artifacts /p:PackageVersion=$nugetversion