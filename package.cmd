@echo off

set COMPRESS="../bin/7zip-bin/7za.exe"
set NUGET="src/.nuget/nuget.exe"

:: Major.Minor
set /p VERSION=Enter version (e.g. 3.0): 
:: YYdayOfYear.BuildNumber
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
%COMPRESS% a -mx9 -r -y "../packages/EventStore-%VERSION%.%BUILD%-net40.zip" *.*
cd ..

:: for some reason nuget doesn't like adding files located in directories underneath it.
move "publish-net40" "bin\nuget"

%NUGET% Pack "bin/nuget/EventStore.nuspec" -Version "%VERSION%.%BUILD%" -OutputDirectory packages
%NUGET% Pack "bin/nuget/EventStore.Serialization.Json.nuspec" -Version "%VERSION%.%BUILD%" -OutputDirectory packages
%NUGET% Pack "bin/nuget/EventStore.Serialization.ServiceStack.nuspec" -Version "%VERSION%.%BUILD%" -OutputDirectory packages
%NUGET% Pack "bin/nuget/EventStore.Persistence.RavenPersistence.nuspec" -Version "%VERSION%.%BUILD%" -OutputDirectory packages
%NUGET% Pack "bin/nuget/EventStore.Persistence.MongoPersistence.nuspec" -Version "%VERSION%.%BUILD%" -OutputDirectory packages
%NUGET% Pack "bin/nuget/EventStore.Logging.Log4Net.nuspec" -Version "%VERSION%.%BUILD%" -OutputDirectory packages
%NUGET% Pack "bin/nuget/EventStore.Logging.NLog.nuspec" -Version "%VERSION%.%BUILD%" -OutputDirectory packages

rmdir /s /q bin\nuget\publish-net40

CALL git checkout "src/proj/VersionAssemblyInfo.cs"
CALL git tag -afm %VERSION%.%BUILD% "%VERSION%.%BUILD%"
echo =====================================================
echo ====* Remember to run: "git push origin --tags" *====
echo =====================================================