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
cd publish-net40
"../bin/7zip-bin/7za.exe" a -mx9 -r -y "../packages/EventStore-%VERSION%.%BUILD%-net40.zip" *.*
cd ..

call build-net35.cmd
cd publish-net35
"../bin/7zip-bin/7za.exe" a -mx9 -r -y "../packages/EventStore-%VERSION%.%BUILD%-net35.zip" *.*
cd ..

:: for some reason nuget doesn't like adding files located in directories underneath it.  v1.4 bug?
move "publish-net35" "bin\nuget"
move "publish-net40" "bin\nuget"

"bin/nuget/nuget.exe" Pack "bin/nuget/eventstore.2.0.nuspec" -Version "%VERSION%.%BUILD%" -OutputDirectory packages
"bin/nuget/nuget.exe" Pack "bin/nuget/eventstore.mongodb.2.0.nuspec" -Version "%VERSION%.%BUILD%" -OutputDirectory packages
"bin/nuget/nuget.exe" Pack "bin/nuget/eventstore.ravendb.2.0.nuspec" -Version "%VERSION%.%BUILD%" -OutputDirectory packages
"bin/nuget/nuget.exe" Pack "bin/nuget/eventstore.json.2.0.nuspec" -Version "%VERSION%.%BUILD%" -OutputDirectory packages
"bin/nuget/nuget.exe" Pack "bin/nuget/eventstore.servicestack.2.0.nuspec" -Version "%VERSION%.%BUILD%" -OutputDirectory packages

rmdir /s /q bin\nuget\publish-net40
rmdir /s /q bin\nuget\publish-net35

CALL git checkout "src/proj/VersionAssemblyInfo.cs"
CALL git tag -afm %VERSION%.%BUILD% "%VERSION%.%BUILD%"
echo Remember to run: git push origin --tags