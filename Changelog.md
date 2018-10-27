# NEventStore versions

## version 6.0.0

Version 6 is not backwards compatible with version 5.x. Updating to NEventStore 6.x without doing some preparation work will result in problems.

### New Features

- dotnet standard 2.0 , dotnet core 2.0 are now supported for the following projects: NEventStore, NEventStore.Domain, NEventStore.Persistence.Sql, NEventStore.Persistence.MongoDb

### Breaking changes

- **Removed Dispatcher and dispatching mechanic, use the PollingClient**: it was marked obsolete in the version 5.x, you should dispatch with other mechanism, like using a PollingClient.
More information on this topic in the issue: [Race condition in sync and async dispatchers can result in subscribers getting commits / events out of order](https://github.com/NEventStore/NEventStore/issues/360).
- **Removed LongCheckpoint class**: checkpoint now is a plain Int64, there is no need to keep a LongCheckpoint class anymore. 
- **PollingClient was removed because it used to depend on Rx**: you can [read more information here](/src/NEventStore/Client/README.MD). The new polling client class is called PollingClient2, this however should be considered as a sample implementation you can use to derive your own.
- **JsonSerializer and BsonSerializer were moved in a separate assembly**: if you need them, you should reference the NEventStore.Serialization.Json assembly or implementing your own serializers that depend on the Json.Net version you need.
- **EventMessage** class is now sealed.
- **OptimistcEventStream throws exceptions if a null message or a message with null body is added to the stream**. Previously if you called Add with null event message or add with an eventmessage with null body, the add operation was ignored without any warning or error. 

### Other Notes

All persistence providers: 

- [MongoDb](https://github.com/NEventStore/NEventStore.Persistence.MongoDB)
- [Sql](https://github.com/NEventStore/NEventStore.Persistence.SQL) 
- [RavenDb](https://github.com/NEventStore/NEventStore.Persistence.RavenDB) - currently not maintained anymore.

are now hosted in their own project. 

Common Domain is now moved in its [own repository](https://github.com/NEventStore/NEventStore.Domain).

## v.5.x.x

Note: Version 5 is not backwards compatible with v4. Updating to v5 without doing some preparation work will result in problems.

### Breaking Changes

1. Underlying schema has changed for all v5 storage engines. In order to migrate a store from v4 to v5 use NEventStore.Migrations
1.The concept of a 'Bucket' has been added as a container for streams allowing multi-tenancy, partitions, multiple-bounded contexts, sagas, etc to be stored in the one store. The API changes have been such that, using extension methods, operations will work on the default bucket, unless a bucket Id has been explicitly supplied. This should mean minimal code changes for the user.
1.Stream Ids are now string based and are limited to 1000 characters.
In the SQL engines the stream Id's are limited to 40 characters and are hashed versions of the actual StreamId.
The hashing function can be overridden during wireup.

### New Features

#### Polling Client

As an alternative to the dispatcher mechanism and improved replay / catch-up story we have implemented a CheckpointNumber in the stores that guarantees ordering across the streams. This number is guaranteed to increment but not guaranteed to be sequential. This allows you to get all Commits from a specific checkpoint and observe new ones. This implementation is polling based (and thus works for all engines) so it doesn't have the same low-latency attributes of the dispatcher mechanism. You can see how to use it here: [https://gist.github.com/damianh/6370328](https://gist.github.com/damianh/6370328) .In this, instead of the store tracking what has been dispatched, the onus is on the client to track what it has seen. And upon restart, start subscribing from what it last saw.

In the future I'd like to see / implement reactive clients that leverage stores that are observable.

### Other Notes

1. Only SQL and MongoDB persistence engines are supported in this release. RavenDB engine will be shipped later.
1. RavenDB and MongoDB persistence engines are now in their own repositories and will have be shipped independently.
