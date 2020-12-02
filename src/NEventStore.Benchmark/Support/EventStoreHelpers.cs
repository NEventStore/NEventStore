namespace NEventStore.Benchmark.Support
{
    internal static class EventStoreHelpers
    {
        internal static IStoreEvents WireupEventStore()
        {
            return Wireup.Init()
               // .LogToOutputWindow(LogLevel.Verbose)
               // .LogToConsoleWindow(LogLevel.Verbose)
               .UsingInMemoryPersistence()
               .InitializeStorageEngine()
#if NET461
               .TrackPerformanceInstance("example")
#endif
               // .HookIntoPipelineUsing(new[] { new AuthorizationPipelineHook() })
               .Build();
        }
    }
}
