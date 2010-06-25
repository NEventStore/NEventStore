@echo off
SET PATH=%PATH%;C:\WINDOWS\Microsoft.NET\Framework\V3.5;

if not exist output ( mkdir output )

echo Compiling
msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=Release

echo Merging
bin\ilmerge-bin\ILMerge.exe /keyfile:src/EventStore.snk /v2 /out:output/EventStore.dll src/proj/EventStore/bin/Release/EventStore.dll src/proj/EventStore.Core/bin/Release/EventStore.Core.dll src/proj/EventStore.Core.Sql/bin/Release/EventStore.Core.Sql.dll src/proj/EventStore.Core.Sql.MsSqlServer/bin/Release/EventStore.Core.Sql.MsSqlServer.dll

echo Done