@ECHO OFF
SETLOCAL

set MSPEC_RUNNER_X86="src/packages/Machine.Specifications-Signed.0.5.3.0/tools/mspec-x86-clr4.exe"

ECHO === Building ===
"C:/WINDOWS/Microsoft.NET/Framework/v4.0.30319/msbuild.exe" /nologo /verbosity:quiet src/EventStore.sln /p:Configuration=Debug

ECHO === In Memory ===
CALL :run_test InMemoryPersistence

ECHO === RDBMS ===
CALL :run_test MsSqlPersistence localhost 0 EventStore "" ""
CALL :run_test SqlitePersistence localhost 0 EventStore "" ""
CALL :run_test SqlCePersistence localhost 0 EventStore "" ""
CALL :run_test AccessPersistence localhost 0 EventStore "" ""
CALL :run_test MySqlPersistence localhost 0 EventStore "" ""
CALL :run_test PostgreSqlPersistence localhost EventStore "" ""
CALL :run_test FirebirdPersistence localhost 3050 EventStore "SYSDBA" "masterkey"

ECHO === Document DBs ===
CALL :run_test MongoPersistence localhost 0 EventStore "" ""
CALL :run_test RavenPersistence localhost 0 EventStore "" ""

ENDLOCAL
GOTO :eof 

:run_test <persistence> <host> <port> <database> <user> <password>
SETLOCAL

SET persistence=%~1
SET host=%~2
SET port=%~3
SET database=%~4
SET user=%~5
SET password=%~6

ECHO ===============
ECHO TESTING: %~1
%MSPEC_RUNNER_X86% "src/tests/EventStore.Persistence.AcceptanceTests/bin/Debug/EventStore.Persistence.AcceptanceTests.dll"
ECHO.

ENDLOCAL