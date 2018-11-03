$configuration = "Release"
$artifacts = "../../artifacts"

choco install gitversion.portable -pre -y

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

dotnet pack ./src/NEventStore/NEventStore.Core.csproj -c $configuration --no-build -o $artifacts /p:PackageVersion=$nugetversion
dotnet pack ./src/NEventStore.Serialization.Json/NEventStore.Serialization.Json.Core.csproj -c $configuration --no-build -o $artifacts /p:PackageVersion=$nugetversion