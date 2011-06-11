@echo off

set /p VERSION=Enter version (e.g. 2.0): 
set /p BUILD=Enter a build (e.g. 11234.17): 
set /p MATURITY=Enter maturity (e.g. Alpha, Beta, RC, Release, etc.): 

echo using System.Reflection; > "src/proj/VersionAssemblyInfo.cs"
echo. >> "src/proj/VersionAssemblyInfo.cs"
echo [assembly: AssemblyVersion("%VERSION%.0.0")] >> "src/proj/VersionAssemblyInfo.cs"
echo [assembly: AssemblyFileVersion("%VERSION%.%BUILD%")] >> "src/proj/VersionAssemblyInfo.cs"
echo //// [assembly: AssemblyInformationalVersion("%VERSION%.%BUILD% %MATURITY%")] >> "src/proj/VersionAssemblyInfo.cs"

if exist packages ( rmdir /s /q packages )
mkdir packages

call build.cmd
cd publish-v4.0
"../bin/7zip-bin/7za.exe" a -mx9 -r -y "../packages/EventStore-%VERSION%.%BUILD%-net40.zip" *.*
cd ..

call build-net35.cmd
cd publish-v3.5
"../bin/7zip-bin/7za.exe" a -mx9 -r -y "../packages/EventStore-%VERSION%.%BUILD%-net35.zip" *.*
cd ..

cd nuget
nuget Pack eventstore.2.0.nuspec -Version %VERSION%.%BUILD% -OutputDirectory ..\packages
nuget Pack eventstore.json.2.0.nuspec -Version %VERSION%.%BUILD% -OutputDirectory ..\packages
nuget Pack eventstore.mongodb.2.0.nuspec -Version %VERSION%.%BUILD% -OutputDirectory ..\packages
nuget Pack eventstore.ravendb.2.0.nuspec -Version %VERSION%.%BUILD% -OutputDirectory ..\packages
nuget Pack eventstore.servicestack.2.0.nuspec -Version %VERSION%.%BUILD% -OutputDirectory ..\packages
cd ..

rmdir /s /q publish-v4.0
rmdir /s /q publish-v3.5

git checkout "src/proj/VersionAssemblyInfo.cs"
git tag -afm %VERSION%.%BUILD% "%VERSION%.%BUILD%"