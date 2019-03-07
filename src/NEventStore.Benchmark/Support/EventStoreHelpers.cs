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
#if !NETSTANDARD1_6 && !NETSTANDARD2_0
               .TrackPerformanceInstance("example")
#endif
               // .HookIntoPipelineUsing(new[] { new AuthorizationPipelineHook() })
               .Build();
        }
    }
}
