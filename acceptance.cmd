@ECHO OFF

ECHO SQL Server
SET persistence=MsSqlPersistence
SET database=EventStore2
"bin/machine.specifications-bin/.NET 4.0/mspec.exe" src/tests/EventStore.Persistence.AcceptanceTests/bin/Debug/EventStore.Persistence.AcceptanceTests.dll

ECHO SQL Server Compact
SET persistence=SqlCePersistence
SET database=EventStore2
"bin/machine.specifications-bin/.NET 4.0/mspec.exe" src/tests/EventStore.Persistence.AcceptanceTests/bin/Debug/EventStore.Persistence.AcceptanceTests.dll

ECHO Access
SET persistence=AccessPersistence
SET database=EventStore2
"bin/machine.specifications-bin/.NET 4.0/mspec.exe" src/tests/EventStore.Persistence.AcceptanceTests/bin/Debug/EventStore.Persistence.AcceptanceTests.dll

ECHO MySQL
SET persistence=MySqlPersistence
SET user=root
SET database=EventStore2
"bin/machine.specifications-bin/.NET 4.0/mspec.exe" src/tests/EventStore.Persistence.AcceptanceTests/bin/Debug/EventStore.Persistence.AcceptanceTests.dll

ECHO PostgreSQL
SET persistence=PostgreSqlPersistence
SET user=postgres
SET database=EventStore2
"bin/machine.specifications-bin/.NET 4.0/mspec.exe" src/tests/EventStore.Persistence.AcceptanceTests/bin/Debug/EventStore.Persistence.AcceptanceTests.dll

ECHO Firebird
SET persistence=FirebirdPersistence
SET user=SYSDBA
SET password=masterkey
SET database="/var/lib/firebird/data/EventStore2.fdb"
"bin/machine.specifications-bin/.NET 4.0/mspec.exe" src/tests/EventStore.Persistence.AcceptanceTests/bin/Debug/EventStore.Persistence.AcceptanceTests.dll