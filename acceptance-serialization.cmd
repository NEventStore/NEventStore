@ECHO OFF
SETLOCAL

ECHO === Building ===
C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=Debug

CALL :run_test Binary
CALL :run_test Gzip
CALL :run_test Rijndael
CALL :run_test Json
CALL :run_test Bson
CALL :run_test ServiceStackJson

ENDLOCAL
GOTO :eof 

:run_test <serializer>
SETLOCAL

SET serializer=%~1

ECHO ===============
ECHO TESTING: %serializer%
"bin/machine.specifications-bin/.NET 4.0/mspec.exe" src/tests/EventStore.Serialization.AcceptanceTests/bin/Debug/EventStore.Serialization.AcceptanceTests.dll
ECHO.

ENDLOCAL