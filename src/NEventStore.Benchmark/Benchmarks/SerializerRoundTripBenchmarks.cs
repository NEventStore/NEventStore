using BenchmarkDotNet.Attributes;
using NEventStore.Benchmark.Support;
using NEventStore.Serialization;
using NEventStore.Serialization.Binary;
using NEventStore.Serialization.Bson;
using NEventStore.Serialization.Json;
using NEventStore.Serialization.MsgPack;

namespace NEventStore.Benchmark.Benchmarks
{
    [Config(typeof(AllowNonOptimized))]
    [SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
    [MemoryDiagnoser]
    [MeanColumn, StdErrorColumn, StdDevColumn, MinColumn, MaxColumn, IterationsColumn]
    public class SerializerRoundTripBenchmarks
    {
        private static readonly byte[] EncryptionKey =
        [
            0x1, 0x2, 0x3, 0x4,
            0x5, 0x6, 0x7, 0x8,
            0x9, 0xa, 0xb, 0xc,
            0xd, 0xe, 0xf, 0x0
        ];

        private ISerialize? _binarySerializer;
        private ISerialize? _bsonSerializer;
        private ISerialize? _jsonSerializer;
        private ISerialize? _gzipJsonSerializer;
        private ISerialize? _gzipBinarySerializer;
        private ISerialize? _msgPackSerializer;
        private ISerialize? _rijndaelBinarySerializer;
        private List<EventMessage>? _payload;

        [Params(10, 100)]
        public int EventCount { get; set; }

        [Params(64, 4096)]
        public int PayloadSizeBytes { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);

            _binarySerializer = CreateBinarySerializer();
            _bsonSerializer = new BsonSerializer();
            _jsonSerializer = new JsonSerializer(null);
            _gzipJsonSerializer = new GzipSerializer(new JsonSerializer(null));
            _gzipBinarySerializer = new GzipSerializer(CreateBinarySerializer());
            _msgPackSerializer = new MsgPackSerializer();
            _rijndaelBinarySerializer = new RijndaelSerializer(CreateBinarySerializer(), EncryptionKey);
            // Keep the matrix deliberately small but representative: plain serializers measure their
            // own object/byte costs, while the gzip and Rijndael wrappers expose extra buffering and
            // copy work on both small metadata-heavy payloads and larger body-heavy payloads.
            _payload = Enumerable.Range(0, EventCount)
                .Select(i =>
                {
                    var message = new EventMessage
                    {
                        Body = new SomeDomainEvent
                        {
                            Value = $"{i:D4}-{new string((char)('a' + (i % 26)), PayloadSizeBytes)}"
                        }
                    };
                    message.Headers["index"] = i;
                    return message;
                })
                .ToList();
        }

        [Benchmark]
        public List<EventMessage>? BinaryRoundTrip()
        {
            var bytes = BinarySerializer.Serialize(Payload);
            return BinarySerializer.Deserialize<List<EventMessage>>(bytes);
        }

        [Benchmark]
        public List<EventMessage>? BsonRoundTrip()
        {
            var bytes = BsonSerializer.Serialize(Payload);
            return BsonSerializer.Deserialize<List<EventMessage>>(bytes);
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

        [Benchmark]
        public List<EventMessage>? GzipBinaryRoundTrip()
        {
            var bytes = GzipBinarySerializer.Serialize(Payload);
            return GzipBinarySerializer.Deserialize<List<EventMessage>>(bytes);
        }

        [Benchmark]
        public List<EventMessage>? MsgPackRoundTrip()
        {
            var bytes = MsgPackSerializer.Serialize(Payload);
            return MsgPackSerializer.Deserialize<List<EventMessage>>(bytes);
        }

        [Benchmark]
        public List<EventMessage>? RijndaelBinaryRoundTrip()
        {
            var bytes = RijndaelBinarySerializer.Serialize(Payload);
            return RijndaelBinarySerializer.Deserialize<List<EventMessage>>(bytes);
        }

#pragma warning disable CS0618
        private static ISerialize CreateBinarySerializer()
        {
            return new BinarySerializer();
        }
#pragma warning restore CS0618

        private ISerialize BinarySerializer => _binarySerializer ?? throw new InvalidOperationException("Serializer has not been initialized.");

        private ISerialize BsonSerializer => _bsonSerializer ?? throw new InvalidOperationException("Serializer has not been initialized.");

        private ISerialize JsonSerializer => _jsonSerializer ?? throw new InvalidOperationException("Serializer has not been initialized.");

        private ISerialize GzipJsonSerializer => _gzipJsonSerializer ?? throw new InvalidOperationException("Serializer has not been initialized.");

        private ISerialize GzipBinarySerializer => _gzipBinarySerializer ?? throw new InvalidOperationException("Serializer has not been initialized.");

        private ISerialize MsgPackSerializer => _msgPackSerializer ?? throw new InvalidOperationException("Serializer has not been initialized.");

        private ISerialize RijndaelBinarySerializer => _rijndaelBinarySerializer ?? throw new InvalidOperationException("Serializer has not been initialized.");

        private List<EventMessage> Payload => _payload ?? throw new InvalidOperationException("Payload has not been initialized.");
    }
}
