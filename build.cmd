@ECHO OFF
SET FRAMEWORK_PATH=C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319
SET PATH=%PATH%;%FRAMEWORK_PATH%;

:target_config
SET TARGET_CONFIG=Release
IF x==%1x GOTO framework_version
SET TARGET_CONFIG=%1

:framework_version
SET FRAMEWORK_VERSION=v4.0
SET ILMERGE_VERSION=v4,%FRAMEWORK_PATH%
IF x==%2x GOTO build
SET FRAMEWORK_VERSION=%2
SET ILMERGE_VERSION=%3

:build
if exist output ( rmdir /s /q output )
mkdir output

echo Compiling / Target: %FRAMEWORK_VERSION% / Config: %TARGET_CONFIG%
msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=%TARGET_CONFIG% /t:Clean
msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=%TARGET_CONFIG% /property:TargetFrameworkVersion=%FRAMEWORK_VERSION%

echo Testing
REM TODO: run existing unit tests and integration tests from command line

echo Merging
mkdir output\bin
SET FILES_TO_MERGE=
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\EventStore\bin\%TARGET_CONFIG%\EventStore.dll"
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\EventStore.Core\bin\%TARGET_CONFIG%\EventStore.Core.dll"
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\EventStore.Core\bin\%TARGET_CONFIG%\protobuf-net.dll"
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\EventStore.SqlStorage\bin\%TARGET_CONFIG%\EventStore.SqlStorage.dll"
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\EventStore.SqlStorage.DynamicSql\bin\%TARGET_CONFIG%\EventStore.SqlStorage.DynamicSql.dll"

REM Echo exclude regex to exclude file *WITHOUT* any line breaks
(echo.|set /p =EventStore.*)>exclude.txt
bin\ilmerge-bin\ILMerge.exe /keyfile:src/EventStore.snk /internalize:"exclude.txt" /xmldocs /targetplatform:%ILMERGE_VERSION% /out:output/bin/EventStore.dll %FILES_TO_MERGE%
del exclude.txt

echo Copying
mkdir output\doc
copy doc\*.* output\doc
copy "lib\protobuf-net\license.txt" "output\doc\protobuf-net license.txt"

echo Cleaning
msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=%TARGET_CONFIG% /t:Clean

echo Done