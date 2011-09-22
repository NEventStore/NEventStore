@echo off

:: Major.Minor
set /p VERSION=Enter version (e.g. 2.0): 
:: YYdayOfYear.BuildNumber
set /p BUILD=Enter a build (e.g. 11234.17): 
set /p MATURITY=Enter maturity (e.g. Alpha, Beta, RC, Release, etc.): 
set PRERELEASE=false
if /i not "%MATURITY%"=="Release" set PRERELEASE=true

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
move "publish-net40" "bin\nuget"
move "publish-net35" "bin\nuget"

"bin/nuget/nuget.exe" Pack "bin/nuget/EventStore.nuspec" -Version "%VERSION%.%BUILD%" -OutputDirectory packages -Prop prerelease="%PRERELEASE%";
"bin/nuget/nuget.exe" Pack "bin/nuget/EventStore.Serialization.Json.nuspec" -Version "%VERSION%.%BUILD%" -OutputDirectory packages -Prop prerelease="%PRERELEASE%";
"bin/nuget/nuget.exe" Pack "bin/nuget/EventStore.Serialization.ServiceStack.nuspec" -Version "%VERSION%.%BUILD%" -OutputDirectory packages -Prop prerelease="%PRERELEASE%";
"bin/nuget/nuget.exe" Pack "bin/nuget/EventStore.Persistence.RavenPersistence.nuspec" -Version "%VERSION%.%BUILD%" -OutputDirectory packages -Prop prerelease="%PRERELEASE%";
"bin/nuget/nuget.exe" Pack "bin/nuget/EventStore.Persistence.MongoPersistence.nuspec" -Version "%VERSION%.%BUILD%" -OutputDirectory packages -Prop prerelease="%PRERELEASE%";
"bin/nuget/nuget.exe" Pack "bin/nuget/EventStore.Logging.Log4Net.nuspec" -Version "%VERSION%.%BUILD%" -OutputDirectory packages -Prop prerelease="%PRERELEASE%";
"bin/nuget/nuget.exe" Pack "bin/nuget/EventStore.Logging.NLog.nuspec" -Version "%VERSION%.%BUILD%" -OutputDirectory packages -Prop prerelease="%PRERELEASE%";

rmdir /s /q bin\nuget\publish-net40
rmdir /s /q bin\nuget\publish-net35

CALL git checkout "src/proj/VersionAssemblyInfo.cs"
CALL git tag -afm %VERSION%.%BUILD% "%VERSION%.%BUILD%"
echo Remember to run: git push origin --tags