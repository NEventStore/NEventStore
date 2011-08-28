cd src
..\nuget\nuget.exe i proj\EventStore.Persistence.MongoPersistence\packages.config -o Packages
..\nuget\nuget.exe i proj\EventStore.Persistence.RavenPersistence\packages.config -o Packages
..\nuget\nuget.exe i proj\EventStore.Serialization.Json\packages.config -o Packages
..\nuget\nuget.exe i proj\EventStore.Serialization.ServiceStack\packages.config -o Packages
..\nuget\nuget.exe i tests\EventStore.Core.UnitTests\packages.config -o Packages
..\nuget\nuget.exe i tests\EventStore.Persistence.AcceptanceTests\packages.config -o Packages
..\nuget\nuget.exe i tests\EventStore.Serialization.AcceptanceTests\packages.config -o Packages
cd ..