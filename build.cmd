@echo off
set FRAMEWORK_PATH=C:/WINDOWS/Microsoft.NET/Framework/v4.0.30319
set PATH=%PATH%;%FRAMEWORK_PATH%;
set ILMERGE="bin/ilmerge-bin/ILMerge.exe"
set MSPEC_RUNNER="src/packages/Machine.Specifications-Signed.0.5.3.0/tools/mspec-clr4.exe"
set MSPEC_RUNNER_X86="src/packages/Machine.Specifications-Signed.0.5.3.0/tools/mspec-x86-clr4.exe"

:target_config
set TARGET_CONFIG=Release
IF x==%1x goto framework_version
set TARGET_CONFIG=%1

:framework_version
set FRAMEWORK_VERSION=v4.0
set ILMERGE_VERSION=v4,%FRAMEWORK_PATH%
set publish=publish-net40

:build
if exist output ( rmdir /s /q output )
if exist output ( rmdir /s /q output )
if exist %publish% ( rmdir /s /q %publish% )
if exist %publish% ( rmdir /s /q %publish% )

mkdir output
mkdir "output\bin"

echo === COMPILING ===
echo Compiling / Target: %FRAMEWORK_VERSION% / Config: %TARGET_CONFIG%
msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=%TARGET_CONFIG% /t:Clean
msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=%TARGET_CONFIG% /p:TargetFrameworkVersion=%FRAMEWORK_VERSION%

echo.
echo === AUTOMATED TESTS ===
echo Unit Tests
%MSPEC_RUNNER% "src/tests/EventStore.Core.UnitTests/bin/%TARGET_CONFIG%/EventStore.Core.UnitTests.dll"

echo Acceptance Tests: Persistence Tests
%MSPEC_RUNNER_X86% "src/tests/EventStore.Persistence.AcceptanceTests/bin/%TARGET_CONFIG%/EventStore.Persistence.AcceptanceTests.dll"

echo Acceptance Tests: Serialization Tests
call acceptance-serialization.cmd

echo.
echo === MERGING ===
echo Merging Primary Assembly
set FILES_TO_MERGE=
set FILES_TO_MERGE=%FILES_TO_MERGE% "src/proj/EventStore/bin/%TARGET_CONFIG%/EventStore.dll"
set FILES_TO_MERGE=%FILES_TO_MERGE% "src/proj/EventStore.Core/bin/%TARGET_CONFIG%/EventStore.Core.dll"
set FILES_TO_MERGE=%FILES_TO_MERGE% "src/proj/EventStore.Serialization/bin/%TARGET_CONFIG%/EventStore.Serialization.dll"
set FILES_TO_MERGE=%FILES_TO_MERGE% "src/proj/EventStore.Persistence.SqlPersistence/bin/%TARGET_CONFIG%/EventStore.Persistence.SqlPersistence.dll"
set FILES_TO_MERGE=%FILES_TO_MERGE% "src/proj/EventStore.Wireup/bin/%TARGET_CONFIG%/EventStore.Wireup.dll"
(echo.|set /p =EventStore.*)>exclude.txt
%ILMERGE% /keyfile:src/EventStore.snk /internalize:"exclude.txt" /xmldocs /wildcards /targetplatform:%ILMERGE_VERSION% /out:output/bin/EventStore.dll %FILES_TO_MERGE%
del exclude.txt

echo Rereferencing Merged Assembly
msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=%TARGET_CONFIG% /t:Clean
msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=%TARGET_CONFIG% /p:ILMerged=true /p:TargetFrameworkVersion=%FRAMEWORK_VERSION%

mkdir output\plugins\persistence\mongo
echo Merging Mongo Persistence
set FILES_TO_MERGE=
set FILES_TO_MERGE=%FILES_TO_MERGE% "src/proj/EventStore.Persistence.MongoPersistence/bin/%TARGET_CONFIG%/EventStore.Persistence.MongoPersistence.dll"
set FILES_TO_MERGE=%FILES_TO_MERGE% "src/proj/EventStore.Persistence.MongoPersistence.Wireup/bin/%TARGET_CONFIG%/EventStore.Persistence.MongoPersistence.Wireup.dll"
echo EventStore.*>exclude.txt
(echo.|set /p =MongoDB.*)>>exclude.txt
%ILMERGE% /keyfile:src/EventStore.snk /internalize:"exclude.txt" /wildcards /targetplatform:%ILMERGE_VERSION% /out:output/plugins/persistence/mongo/EventStore.Persistence.MongoPersistence.dll %FILES_TO_MERGE%
del exclude.txt
copy "src\proj\EventStore.Persistence.MongoPersistence\bin\%TARGET_CONFIG%\MongoDB*.dll" "output\plugins\persistence\mongo"

mkdir output\plugins\persistence\raven
echo Merging Raven Persistence
set FILES_TO_MERGE=
set FILES_TO_MERGE=%FILES_TO_MERGE% "src/proj/EventStore.Persistence.RavenPersistence/bin/%TARGET_CONFIG%/EventStore.Persistence.RavenPersistence.dll"
set FILES_TO_MERGE=%FILES_TO_MERGE% "src/proj/EventStore.Persistence.RavenPersistence.Wireup/bin/%TARGET_CONFIG%/EventStore.Persistence.RavenPersistence.Wireup.dll"
echo EventStore.*>exclude.txt
(echo.|set /p =Raven.*)>>exclude.txt
%ILMERGE% /keyfile:src/EventStore.snk /internalize:"exclude.txt" /wildcards /targetplatform:%ILMERGE_VERSION% /out:output/plugins/persistence/raven/EventStore.Persistence.RavenPersistence.dll %FILES_TO_MERGE%
del exclude.txt
copy "src\proj\EventStore.Persistence.RavenPersistence\bin\%TARGET_CONFIG%\Raven*.dll" "output\plugins\persistence\raven"

mkdir output\plugins\serialization\json-net
echo Merging Newtonsoft Json.NET Serialization
set FILES_TO_MERGE=
set FILES_TO_MERGE=%FILES_TO_MERGE% "src/proj/EventStore.Serialization.Json/bin/%TARGET_CONFIG%/EventStore.Serialization.Json.dll"
set FILES_TO_MERGE=%FILES_TO_MERGE% "src/proj/EventStore.Serialization.Json/bin/%TARGET_CONFIG%/Newtonsoft.Json*.dll"
set FILES_TO_MERGE=%FILES_TO_MERGE% "src/proj/EventStore.Serialization.Json.Wireup/bin/%TARGET_CONFIG%/EventStore.Serialization.Json.Wireup.dll"
(echo.|set /p =EventStore.*)>>exclude.txt
%ILMERGE% /keyfile:src/EventStore.snk /internalize:"exclude.txt" /wildcards /targetplatform:%ILMERGE_VERSION% /out:output/plugins/serialization/json-net/EventStore.Serialization.Json.dll %FILES_TO_MERGE%
del exclude.txt

mkdir output\plugins\serialization\servicestack
echo Merging ServiceStack.Text Serialization
set FILES_TO_MERGE=
set FILES_TO_MERGE=%FILES_TO_MERGE% "src/proj/EventStore.Serialization.ServiceStack/bin/%TARGET_CONFIG%/EventStore.Serialization.ServiceStack.dll"
set FILES_TO_MERGE=%FILES_TO_MERGE% "src/proj/EventStore.Serialization.ServiceStack/bin/%TARGET_CONFIG%/ServiceStack.Text.dll"
set FILES_TO_MERGE=%FILES_TO_MERGE% "src/proj/EventStore.Serialization.ServiceStack.Wireup/bin/%TARGET_CONFIG%/EventStore.Serialization.ServiceStack.Wireup.dll"
(echo.|set /p =EventStore.*)>>exclude.txt
%ILMERGE% /keyfile:src/EventStore.snk /internalize:"exclude.txt" /wildcards /targetplatform:%ILMERGE_VERSION% /out:output/plugins/serialization/servicestack/EventStore.Serialization.ServiceStack.dll %FILES_TO_MERGE%
del exclude.txt

mkdir output\plugins\logging\nlog
copy "src\proj\EventStore.Logging.NLog\bin\%TARGET_CONFIG%\EventStore.Logging.NLog.*" "output\plugins\logging\nlog"
copy "src\proj\EventStore.Logging.NLog\bin\%TARGET_CONFIG%\NLog.dll" "output\plugins\logging\nlog"

mkdir output\plugins\logging\log4net
copy "src\proj\EventStore.Logging.Log4Net\bin\%TARGET_CONFIG%\EventStore.Logging.Log4Net.*" "output\plugins\logging\log4net"
copy "src\proj\EventStore.Logging.Log4Net\bin\%TARGET_CONFIG%\log4net.dll" "output\plugins\logging\log4net"

echo.
echo === FINALIZING ===
echo Copying
mkdir "output\doc"
copy "doc\*.*" "output\doc"

move output %publish%

echo.
echo === CLEANUP ===
echo Cleaning Build
msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=%TARGET_CONFIG% /t:Clean

echo.
echo === DONE ===