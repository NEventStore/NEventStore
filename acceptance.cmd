@ECHO OFF
SETLOCAL

ECHO === RDBMS ===
CALL :run_test MsSqlPersistence localhost 0 EventStore2 "" ""
CALL :run_test SqlitePersistence localhost 0 EventStore2 "" ""
CALL :run_test SqlCePersistence localhost 0 EventStore2 "" ""
CALL :run_test AccessPersistence localhost 0 EventStore2 "" ""
CALL :run_test MySqlPersistence localhost 0 EventStore2 root ""
CALL :run_test PostgreSqlPersistence localhost 0 EventStore2 postgres ""
CALL :run_test FirebirdPersistence localhost 0 /var/lib/firebird/data/EventStore2.fdb SYSDBA masterkey

ECHO === Document DBs ===
CALL :run_test MongoPersistence localhost 0 EventStore2 "" ""

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
"bin/machine.specifications-bin/.NET 4.0/mspec.exe" src/tests/EventStore.Persistence.AcceptanceTests/bin/Debug/EventStore.Persistence.AcceptanceTests.dll
ECHO.

ENDLOCAL