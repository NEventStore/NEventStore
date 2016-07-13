##NEventStore versions

###v.6.0.0

####Breaking changes

- Removed Dispatcher, you should dispatch with other mechanism, ex with a PollingClient
- Removed LongCheckpoint, checkpoint now is a plain Int64, there is no need to keep a LongCheckpoint class anymore. 
- Removed PollingClient because it depends on Rx, you can [read more information here](/src/NEventStore/Client/README.MD)
- JsonSerializer and BsonSerializer are now in a different assembly, if you need them you should reference the NEventStore.Serialization.Json assembly or implementing your serializer to avoid to depend to the same Json.Net version of the serializers

