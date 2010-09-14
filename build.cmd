@echo off
SET PATH=%PATH%;C:\WINDOWS\Microsoft.NET\Framework\V3.5;

if exist output ( rmdir /s /q output )
mkdir output

echo Compiling
msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=Release /t:Clean
msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=Release

echo Copying
copy "src\proj\EventStore.Core.SqlStorage.MsSql\bin\Release\*.sql" output
copy "src\proj\EventStore.Core.SqlStorage.MySql\bin\Release\*.sql" output
copy "src\proj\EventStore.Core.SqlStorage.Sqlite\bin\Release\*.sql" output
copy "src\proj\EventStore.Core.SqlStorage.Postgresql\bin\Release\*.sql" output

echo Merging
SET FILES_TO_MERGE=
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\EventStore\bin\Release\EventStore.dll"
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\EventStore.Core\bin\Release\EventStore.Core.dll"
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\EventStore.Core.SqlStorage\bin\Release\EventStore.Core.SqlStorage.dll"
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\EventStore.Core.SqlStorage.MsSql\bin\Release\EventStore.Core.SqlStorage.MsSql.dll"
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\EventStore.Core.SqlStorage.MySql\bin\Release\EventStore.Core.SqlStorage.MySql.dll"
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\EventStore.Core.SqlStorage.Sqlite\bin\Release\EventStore.Core.SqlStorage.Sqlite.dll"
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src\proj\EventStore.Core.SqlStorage.Postgresql\bin\Release\EventStore.Core.SqlStorage.Postgresql.dll"

bin\ilmerge-bin\ILMerge.exe /keyfile:src\EventStore.snk /v2 /xmldocs /out:output\EventStore.dll %FILES_TO_MERGE%

echo Cleaning
msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=Release /t:Clean

echo Done