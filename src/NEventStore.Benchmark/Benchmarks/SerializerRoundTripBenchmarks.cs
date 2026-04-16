using BenchmarkDotNet.Attributes;
using NEventStore.Benchmark.Support;
using NEventStore.Serialization;
using NEventStore.Serialization.Json;

namespace NEventStore.Benchmark.Benchmarks
{
    [Config(typeof(AllowNonOptimized))]
    [SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
    [MemoryDiagnoser]
    [MeanColumn, StdErrorColumn, StdDevColumn, MinColumn, MaxColumn, IterationsColumn]
    public class SerializerRoundTripBenchmarks
    {
        private ISerialize? _jsonSerializer;
        private ISerialize? _gzipJsonSerializer;
        private List<EventMessage>? _payload;

        [Params(10, 100)]
        public int EventCount { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _jsonSerializer = new JsonSerializer(null);
            _gzipJsonSerializer = new GzipSerializer(new JsonSerializer(null));
            _payload = Enumerable.Range(0, EventCount)
                .Select(i =>
                {
                    var message = new EventMessage { Body = new SomeDomainEvent { Value = $"event-{i}" } };
                    message.Headers["index"] = i;
                    return message;
                })
                .ToList();
        }

        [Benchmark]
        public List<EventMessage>? JsonRoundTrip()
        {
            var bytes = JsonSerializer.Serialize(Payload);
            return JsonSerializer.Deserialize<List<EventMessage>>(bytes);
        }

        [Benchmark]
        public List<EventMessage>? GzipJsonRoundTrip()
        {
            var bytes = GzipJsonSerializer.Serialize(Payload);
            return GzipJsonSerializer.Deserialize<List<EventMessage>>(bytes);
        }

        private ISerialize JsonSerializer => _jsonSerializer ?? throw new InvalidOperationException("Serializer has not been initialized.");

        private ISerialize GzipJsonSerializer => _gzipJsonSerializer ?? throw new InvalidOperationException("Serializer has not been initialized.");

        private List<EventMessage> Payload => _payload ?? throw new InvalidOperationException("Payload has not been initialized.");
    }
}
