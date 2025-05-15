# NEventStore Versions

## 10.1.1

### BugFix

- Fixed Assemblies Version Numbers (AssemblyInfo files).

## 10.1.0

- Improved `IEventStream` interface: `CommitChanges()` and `CommitChangesAsync()` now return `ICommit` instead of `void`.
- Updated MessagePack serializer to 3.1.3

### Breaking Changes

- `IEventStream.CommitChanges()` and `IEventStream.CommitChangesAsync()` now return `ICommit` instead of `void`.

## 10.0.1

### BugFix

- Async Pipeline Hooks: initialization and PreCommit/PostCommit invocation bugs [#516](https://github.com/NEventStore/NEventStore/issues/516)

## 10.0.0

- Async Methods to read from and write to streams (IStoreEvents, IEventStream, IPersistStreams, IPersistStreamsAsync, ICommitEventsAsync, IAccessSnapshotsAsync). [#513](https://github.com/NEventStore/NEventStore/issues/513)
  - methods that read from a stream in an async way follow the Observer pattern and requires you to pass in an `IAsyncObservable` that will receive data as soon as they are available.
- Async Pipeline Hooks (IPipelineHookAsync). [#515](https://github.com/NEventStore/NEventStore/issues/515)
- AsyncPollingClient: a new polling client implementation that uses Async interfaces. [#505](https://github.com/NEventStore/NEventStore/issues/505)
- Removed the BinarySerializer (BinaryFormatter) from the core package and moved it to its own package [#510](https://github.com/NEventStore/NEventStore/issues/510)
- Improved comments and added more nullability checks.
- Minor performance improvements.
- Updated Testing Packages (NUnit, FluentAssertions, Microsoft.NET.Test and so on...).

### Breaking Changes

- `PersistStreamsExtensions.GetFrom(IPersistStreams, DateTime)` and `PersistStreamsExtensions.GetFromTo(IPersistStreams, DateTime, DateTime)` extension methods have been removed: they had inconsistent behavior with the other GetFrom(checkpointToken) methods, 
  they were getting data from the default bucket only.
- `PipelineHooksAwarePersistanceDecorator` renamed to `PipelineHooksAwarePersistStreamsDecorator`.
- `IPipelineHook.Select` method renamed to `IPipelineHook.SelectCommit`.
- `BinarySerializer` moved to its own package: `NEventStore.Serialization.Binary`.
  - for net8.0+ call `AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);` to enable unsafe BinaryFormatter usage.
- Improved many method signature with nullability annotations.
- `Wireup.With()` renamed `Wireup.Register()`.
- `OptimisticEventStream` constructors replaced by initialization functions:
  - `new OptimisticEventStream(string bucketId, string streamId, ICommitEvents persistence, int minRevision, int maxRevision)` -> `new OptimisticEventStream(string bucketId, string streamId, ICommitEvents persistence).Initialize(int minRevision, int maxRevision)`.
  - `new OptimisticEventStream(ISnapshot snapshot, ICommitEvents persistence, int maxRevision)` -> `new OptimisticEventStream(string bucketId, string streamId, ICommitEvents persistence).Initialize(ISnapshot snapshot, int maxRevision)`.

## 9.2.0

- Updated nuget packages to include symbol packages and more information.
- Updated Newtonsoft.Bson 13.0.3
- Added MessagePack serializer, thanks to [@pvagnozzi](https://github.com/pvagnozzi)
- Improved comments and removed some compilation warnings.

## 9.1.1

- Fixed `build.ps1` script to correctly update Assembly Version number before building.
- Updated Readme with how Versioning works.

## 9.1.0

- Support the following Target Frameworks only: netstandard2.0, net462.	
- Updated Newtonsoft.Json 13.0.3

## 9.0.1 

- Added documentation files to NuGet packages (improved intellisense support) [#496](https://github.com/NEventStore/NEventStore/issues/496)

## 9.0.0

- Added support for .net 6 [#493](https://github.com/NEventStore/NEventStore/issues/493).
- Change / Optimization: Commit and CommitAttempt do not create internal read-only collections anymore, it can be useless given that we can change properties of events.
- NEventStore.Serialization.Json: accepts a JsonSerializerSettings to configure the serializer.

## 8.0.0

- Added support for .net 5 [#489](https://github.com/NEventStore/NEventStore/issues/489).
- Added support for .net framework 4.6.1.
- Fixed InMemoryPersistenceEngine.AddSnapshot() behavior: adding multiple snapshots for the same tuple bucketId, streamId, streamRevision is not allowed; the updated snapshot will be ignored [#484](https://github.com/NEventStore/NEventStore/pull/484).
- Logging infrastructure switched to [Microsoft.Extensions.Logging](https://docs.microsoft.com/en-us/dotnet/core/extensions/logging) [#454](https://github.com/NEventStore/NEventStore/issues/454), [#488](https://github.com/NEventStore/NEventStore/pull/488).
- Reviewed Exception (and logging) messages: many of those that refer to a StreamId should also provide BucketId information [#480](https://github.com/NEventStore/NEventStore/issues/480)

### Breaking Changes

- Dropped support for .NET Framework 4.5, only .NET 4.6.1+ will be supported in 8.x. .NET Framework support will be dropped in a future revision.
- Logging switched to Microsoft.Extensions.Logging, old logging code and configuration functions have been removed.

## 7.0.0

- The IPersistStreams interface got some major changes:
	- Added new `GetFromTo(Int64, Int64)` and `GetFromTo(string, Int64, Int64)` methods to the IPersistStreams interface.
	- Extension methods `PersistStreamsExtensions.GetFrom(DateTime)` and `PersistStreamsExtensions.GetFromTo(DateTime, DateTime)` were marked obsolete and will be removed.
	- A new PersistStreamsExtensions.GetCommit(Int64) method was added to retrieve a single commit [#445](https://github.com/NEventStore/NEventStore/issues/445).
- PollingClient was moved to its own NEventStore.PollingClient NuGet package [#467](https://github.com/NEventStore/NEventStore/issues/467).
- Added more information to the DuplicateCommitException error message (StreamId and BucketId), also the information provided by the Persistence providers will be reviewed [#372](https://github.com/NEventStore/NEventStore/issues/372).

### Breaking Changes

- The default value of 0 has been removed from the `IPersistStreams.GetFrom(Int64)` method.
- Removed the almost useless `GetFromStart()` extension method: use `IPersistStream.GetFrom(0)`.
- Bson serializer was moved from NEventStore.Serialization.Json to its own package: `NEventStore.Serialization.Bson`. Closes: [#479](https://github.com/NEventStore/NEventStore/issues/479).
- PollingClient was moved to its own package: add a reference to NEventStore.PollingClient NuGet package. Also the namespace was changed from NEventStore.Client to NEventStorePollingClient.

## 6.1.0

Enlist in ambient transaction has been removed from the mail library and added to the persistence drivers implementations, each driver has its own way to support, enable or disable the feature. As of now this change will mainly impact Microsoft SQL Server users, because all other persistence plugins didn't use transactions at all.

All the transactions (or their suppression) should be explicitly managed by the user.

Minor optimizations were made if no pipeline hooks are used.

### Breaking Changes

- `PipelineHookBase`: changed the way the Dispose pattern was implemented to be compliant with the framework guidelines. Move all the Dispose logic to the overridden Dispose(bool disposing) method of your pipeline hook class.
- `OptimisticPipelineHook` optimization is not configured and enabled by default (if not enlisting in ambient transactions) anymore; it now must be explicitly enabled calling UseOptimisticPipelineHook() when configuring NEventStore. Do not use it if you plan to use transactions. To restore the previous behavior call .UseOptimisticPipelineHook() when configuring NEventStore.
- `EnlistInAmbientTransaction` has been removed from the core NEventStore library. It will be added to specific persistence drivers implementations.

## 6.0.0

__Version 6.x is not backwards compatible with version 5.x.__ Updating to NEventStore 6.x without doing some preparation work will result in problems.

### New Features

- dotnet standard 2.0 , dotnet core 2.0 are now supported for the following projects: NEventStore, NEventStore.Domain, NEventStore.Persistence.Sql, NEventStore.Persistence.MongoDb

### Breaking Changes

- **Removed Dispatcher and dispatching mechanic, use the PollingClient**: it was marked obsolete in the version 5.x, you should dispatch events with other mechanisms, like using a PollingClient.
More information on this topic in the issue: [Race condition in sync and async dispatchers can result in subscribers getting commits / events out of order](https://github.com/NEventStore/NEventStore/issues/360).
- **Removed LongCheckpoint class**: checkpoint now is a plain Int64, there is no need to keep a LongCheckpoint class anymore. 
- **PollingClient was removed because it used to depend on Rx**: you can [read more information here](src/NEventStore/Client/README.MD). The new polling client class is called PollingClient2, this however should be considered as a sample implementation you can use to derive your own.
- **JsonSerializer and BsonSerializer were moved in a separate assembly**: if you need them, you should reference the NEventStore.Serialization.Json assembly or implement your own serializers that depend on the Json.Net version you need.
- **EventMessage** class is now sealed.
- **`OptimisticEventStream` throws exceptions if a null message or a message with null body is added to the stream**. Previously if you called Add with null event message or add with an event message with null body, the add operation was ignored without any warning or error. 

## 6.0.0-rc-1

New features:

- improved logging performances ([#468](https://github.com/NEventStore/NEventStore/issues/468)).

Bug fixed:

- adding events in the middle of a commit should throw ConcurrencyException ([#420](https://github.com/NEventStore/NEventStore/issues/420)).

## 6.0.0-rc-0

__Version 6.x is not backwards compatible with version 5.x.__ Updating to NEventStore 6.x without doing some preparation work will result in problems.

### New Features

- dotnet standard 2.0 , dotnet core 2.0 are now supported for the following projects: NEventStore, NEventStore.Domain, NEventStore.Persistence.Sql, NEventStore.Persistence.MongoDb

### Breaking changes

- **Removed Dispatcher and dispatching mechanic, use the PollingClient**: it was marked obsolete in the version 5.x, you should dispatch events with other mechanisms, like using a PollingClient.
More information on this topic in the issue: [Race condition in sync and async dispatchers can result in subscribers getting commits / events out of order](https://github.com/NEventStore/NEventStore/issues/360).
- **Removed LongCheckpoint class**: checkpoint now is a plain Int64, there is no need to keep a LongCheckpoint class anymore. 
- **PollingClient was removed because it used to depend on Rx**: you can [read more information here](src/NEventStore/Client/README.MD). The new polling client class is called PollingClient2, this however should be considered as a sample implementation you can use to derive your own.
- **JsonSerializer and BsonSerializer were moved in a separate assembly**: if you need them, you should reference the NEventStore.Serialization.Json assembly or implement your own serializers that depend on the Json.Net version you need.
- **EventMessage** class is now sealed.
- **OptimisticEventStream throws exceptions if a null message or a message with null body is added to the stream**. Previously if you called Add with null event message or add with an event message with null body, the add operation was ignored without any warning or error. 

### Other Notes

All persistence providers: 

- [MongoDb](https://github.com/NEventStore/NEventStore.Persistence.MongoDB)
- [Sql](https://github.com/NEventStore/NEventStore.Persistence.SQL) 
- [RavenDb](https://github.com/NEventStore/NEventStore.Persistence.RavenDB) - currently not maintained anymore.

are now hosted in their own project. 

Common Domain is now moved in its [own repository](https://github.com/NEventStore/NEventStore.Domain).

## 5.x.x

Note: Version 5 is not backwards compatible with v4. Updating to v5 without doing some preparation work will result in problems.

### Breaking Changes

1. Underlying schema has changed for all v5 storage engines. In order to migrate a store from v4 to v5 use NEventStore.Migrations
1.The concept of a 'Bucket' has been added as a container for streams allowing multi-tenancy, partitions, multiple-bounded contexts, sagas, etc to be stored in the one store. The API changes have been such that, using extension methods, operations will work on the default bucket, unless a bucket Id has been explicitly supplied. This should mean minimal code changes for the user.
1.Stream Ids are now string based and are limited to 1000 characters.
In the SQL engines the stream Id's are limited to 40 characters and are hashed versions of the actual StreamId.
The hashing function can be overridden during wire-up.

### New Features

#### Polling Client

As an alternative to the dispatcher mechanism and improved replay / catch-up story we have implemented a CheckpointNumber in the stores that guarantees ordering across the streams. This number is guaranteed to increment but not guaranteed to be sequential. This allows you to get all Commits from a specific checkpoint and observe new ones. This implementation is polling based (and thus works for all engines) so it doesn't have the same low-latency attributes of the dispatcher mechanism. You can see how to use it here: [https://gist.github.com/damianh/6370328](https://gist.github.com/damianh/6370328) .In this, instead of the store tracking what has been dispatched, the onus is on the client to track what it has seen. And upon restart, start subscribing from what it last saw.

In the future I'd like to see / implement reactive clients that leverage stores that are observable.

### Other Notes

1. Only SQL and MongoDB persistence engines are supported in this release. RavenDB engine will be shipped later.
1. RavenDB and MongoDB persistence engines are now in their own repositories and will have be shipped independently.
