##NEventStore versions

###v.6.0.0

####Breaking changes

- Removed Dispatcher, you should dispatch with other mechanism, ex with a PollingClient
- Removed LongCheckpoint, checkpoint now is a plain Int64, there is no need to keep a LongCheckpoint class anymore. 
- Removed PollingClient because it depends on Rx, you can [read more information here](/src/NEventStore/Client/README.MD)

