@echo off
SET PATH=%PATH%;C:\WINDOWS\Microsoft.NET\Framework\V3.5;

if not exist output ( mkdir output )

echo Compiling
msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=Release /t:Clean
msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=Release

echo Merging

SET FILES_TO_MERGE=
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src/proj/EventStore/bin/Release/EventStore.dll"
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src/proj/EventStore.Core/bin/Release/EventStore.Core.dll"
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src/proj/EventStore.Core.Sql/bin/Release/EventStore.Core.Sql.dll"
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src/proj/EventStore.Core.Sql.MsSql/bin/Release/EventStore.Core.Sql.MsSql.dll"
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src/proj/EventStore.Core.Sql.MySql/bin/Release/EventStore.Core.Sql.MySql.dll"
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src/proj/EventStore.Core.Sql.Sqlite/bin/Release/EventStore.Core.Sql.Sqlite.dll"
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src/proj/EventStore.Core.Sql.Postgresql/bin/Release/EventStore.Core.Sql.Postgresql.dll"

bin\ilmerge-bin\ILMerge.exe /keyfile:src/EventStore.snk /v2 /xmldocs /out:output/EventStore.dll %FILES_TO_MERGE%

echo Cleaning
msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=Release /t:Clean

echo Done