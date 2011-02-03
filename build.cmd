@echo off
set FRAMEWORK_PATH=C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319
set PATH=%PATH%;%FRAMEWORK_PATH%;

:target_config
set TARGET_CONFIG=Release
IF x==%1x goto framework_version
set TARGET_CONFIG=%1

:framework_version
set FRAMEWORK_VERSION=v4.0
set ILMERGE_VERSION=v4,%FRAMEWORK_PATH%
if x==%2x goto build
set FRAMEWORK_VERSION=%2
set ILMERGE_VERSION=%3

:build
if exist output ( rmdir /s /q output )
if exist output ( rmdir /s /q output )
if exist publish ( rmdir /s /q publish )
if exist publish ( rmdir /s /q publish )

mkdir output
mkdir output\bin

echo === COMPILING ===
echo Compiling / Target: %FRAMEWORK_VERSION% / Config: %TARGET_CONFIG%
msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=%TARGET_CONFIG% /t:Clean
msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=%TARGET_CONFIG% /property:TargetFrameworkVersion=%FRAMEWORK_VERSION%

echo.
echo === TESTS ===
echo Unit Tests
"bin/machine.specifications-bin/.NET 4.0/mspec.exe" src/tests/EventStore.Core.UnitTests/bin/%TARGET_CONFIG%/EventStore.Core.UnitTests.dll
echo Acceptance Tests
"bin/machine.specifications-bin/.NET 4.0/mspec.exe" src/tests/EventStore.Persistence.AcceptanceTests/bin/%TARGET_CONFIG%/EventStore.Persistence.AcceptanceTests.dll
call acceptance-serialization.cmd


echo.
echo === MERGING ===
echo Merging Primary Assembly
set FILES_TO_MERGE=
set FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\EventStore\bin\%TARGET_CONFIG%\EventStore.dll"
set FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\EventStore.Core\bin\%TARGET_CONFIG%\EventStore.Core.dll"
set FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\EventStore.Serialization\bin\%TARGET_CONFIG%\EventStore.Serialization.dll"
set FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\EventStore.Persistence.SqlPersistence\bin\%TARGET_CONFIG%\EventStore.Persistence.SqlPersistence.dll"
(echo.|set /p =EventStore.*)>exclude.txt
bin\ilmerge-bin\ILMerge.exe /keyfile:src/EventStore.snk /internalize:"exclude.txt" /xmldocs /wildcards /targetplatform:%ILMERGE_VERSION% /out:output/bin/EventStore.dll %FILES_TO_MERGE%
del exclude.txt

echo Rereferencing Merged Assembly
msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=%TARGET_CONFIG% /t:Clean
msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=%TARGET_CONFIG% /property:TargetFrameworkVersion=%FRAMEWORK_VERSION%

echo Merging Mongo Persistence
set FILES_TO_MERGE=
set FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\EventStore.Persistence.MongoPersistence\bin\%TARGET_CONFIG%\EventStore.Persistence.MongoPersistence.dll"
set FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\EventStore.Persistence.MongoPersistence\bin\%TARGET_CONFIG%\MongoDB*.dll"
echo EventStore.*>exclude.txt
(echo.|set /p =MongoDB.*)>>exclude.txt
bin\ilmerge-bin\ILMerge.exe /keyfile:src/EventStore.snk /internalize:"exclude.txt" /xmldocs /wildcards /targetplatform:%ILMERGE_VERSION% /out:output/bin/EventStore.Persistence.MongoPersistence.dll %FILES_TO_MERGE%
del exclude.txt

echo Merging Json Serialization
set FILES_TO_MERGE=
set FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\EventStore.Serialization.Json\bin\%TARGET_CONFIG%\EventStore.Serialization.Json.dll"
set FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\EventStore.Serialization.Json\bin\%TARGET_CONFIG%\Newtonsoft.Json*.dll"
(echo.|set /p =EventStore.*)>>exclude.txt
bin\ilmerge-bin\ILMerge.exe /keyfile:src/EventStore.snk /internalize:"exclude.txt" /xmldocs /wildcards /targetplatform:%ILMERGE_VERSION% /out:output/bin/EventStore.Serialization.Json.dll %FILES_TO_MERGE%
del exclude.txt

echo.
echo === FINALIZING ===
echo Copying
mkdir output\doc
copy doc\*.* output\doc
copy "lib\Json.NET\license.txt" "output\doc\Newtonsoft Json.NET license.txt"

move output publish

echo.
echo === CLEANUP ===
echo Cleaning Build
msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=%TARGET_CONFIG% /t:Clean

echo.
echo === DONE ===