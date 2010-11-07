@echo off
SET FRAMEWORK_PATH=C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319
SET PATH=%PATH%;%FRAMEWORK_PATH%;

if exist output ( rmdir /s /q output )
mkdir output

echo Compiling
msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=Release /t:Clean
msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=Release /property:TargetFrameworkVersion=v3.5

echo Copying
copy "doc\*.sql" output

echo Merging

SET FILES_TO_MERGE=
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\EventStore\bin\Release\EventStore.dll"
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\EventStore.Core\bin\Release\EventStore.Core.dll"
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\EventStore.SqlStorage\bin\Release\EventStore.SqlStorage.dll"
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\EventStore.SqlStorage.DynamicSql\bin\Release\EventStore.SqlStorage.DynamicSql.dll"

bin\ilmerge-bin\ILMerge.exe /keyfile:src/EventStore.snk /xmldocs /v2 /out:output/EventStore.dll %FILES_TO_MERGE%

echo Cleaning
msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=Release /t:Clean

echo Done