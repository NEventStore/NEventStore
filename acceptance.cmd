@ECHO OFF

ECHO SQL Server
SET persistence=MsSqlPersistence
SET host=localhost
SET database=EventStore2
"bin/machine.specifications-bin/.NET 4.0/mspec.exe" src/tests/EventStore.Persistence.AcceptanceTests/bin/Debug/EventStore.Persistence.AcceptanceTests.dll

ECHO SQL Server Compact
SET persistence=SqlCePersistence
SET host=localhost
SET database=EventStore2
"bin/machine.specifications-bin/.NET 4.0/mspec.exe" src/tests/EventStore.Persistence.AcceptanceTests/bin/Debug/EventStore.Persistence.AcceptanceTests.dll

ECHO Access
SET persistence=AccessPersistence
SET host=localhost
SET database=EventStore2
"bin/machine.specifications-bin/.NET 4.0/mspec.exe" src/tests/EventStore.Persistence.AcceptanceTests/bin/Debug/EventStore.Persistence.AcceptanceTests.dll

ECHO MySQL
SET persistence=MySqlPersistence
SET host=localhost
SET user=root
SET database=EventStore2
"bin/machine.specifications-bin/.NET 4.0/mspec.exe" src/tests/EventStore.Persistence.AcceptanceTests/bin/Debug/EventStore.Persistence.AcceptanceTests.dll

ECHO PostgreSQL
SET persistence=PostgreSqlPersistence
SET host=localhost
SET user=postgres
SET database=EventStore2
"bin/machine.specifications-bin/.NET 4.0/mspec.exe" src/tests/EventStore.Persistence.AcceptanceTests/bin/Debug/EventStore.Persistence.AcceptanceTests.dll

ECHO Firebird
SET persistence=FirebirdPersistence
SET host=localhost
SET user=SYSDBA
SET password=masterkey
SET database="/var/lib/firebird/data/EventStore2.fdb"
"bin/machine.specifications-bin/.NET 4.0/mspec.exe" src/tests/EventStore.Persistence.AcceptanceTests/bin/Debug/EventStore.Persistence.AcceptanceTests.dll