@ECHO OFF
SETLOCAL

ECHO === Building ===
C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\msbuild /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=Debug

CALL :run_test Binary
CALL :run_test Compressed
REM CALL :run_test Xml
CALL :run_test Json
CALL :run_test Bson
REM CALL :run_test ProtocolBuffers

PAUSE

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