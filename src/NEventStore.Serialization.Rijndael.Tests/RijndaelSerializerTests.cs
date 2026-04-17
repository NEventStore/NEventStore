using FluentAssertions;
using NEventStore.Persistence.AcceptanceTests;
using NEventStore.Persistence.AcceptanceTests.BDD;
using NEventStore.Serialization.Binary;
#if MSTEST
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
#if XUNIT
using Xunit;
using Xunit.Should;
#endif

#pragma warning disable IDE1006 // Naming Styles

namespace NEventStore.Serialization.Rijndael.Tests
{
#if MSTEST
    [TestClass]
#endif
    public class when_round_tripping_a_large_payload_with_rijndael_serializer : SpecificationBase
    {
        private static readonly byte[] EncryptionKey =
        [
            0x1, 0x2, 0x3, 0x4,
            0x5, 0x6, 0x7, 0x8,
            0x9, 0xa, 0xb, 0xc,
            0xd, 0xe, 0xf, 0x0
        ];

        private readonly EventMessage _message =
            new()
            {
                Body = new SimpleMessage
                {
                    Id = Guid.NewGuid(),
                    Created = new DateTime(2026, 4, 17, 0, 0, 0, DateTimeKind.Utc),
                    Value = new string('x', 128 * 1024),
                    Count = 42
                }
            };

        private byte[]? _serialized;
        private EventMessage? _deserialized;

        protected override void Context()
        {
#if NET8_0_OR_GREATER
            AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);
#endif
            _message.Headers["payload"] = "large";
        }

        protected override void Because()
        {
#pragma warning disable CS0618
            var serializer = new RijndaelSerializer(new BinarySerializer(), EncryptionKey);
#pragma warning restore CS0618
            _serialized = serializer.Serialize(_message);
            // Force the IV prefix to arrive in tiny chunks so the regression fails if the wrapper
            // assumes a single Stream.Read call will always return the full IV before decryption.
            using var stream = new ChunkedReadStream(new MemoryStream(_serialized!), 3);
            _deserialized = serializer.Deserialize<EventMessage>(stream);
        }

        [Fact]
        public void should_round_trip_the_large_message_body()
        {
            var payload = _deserialized!.Body.Should().BeOfType<SimpleMessage>().Subject;
            payload.Value.Should().Be(((SimpleMessage)_message.Body).Value);
            payload.Count.Should().Be(((SimpleMessage)_message.Body).Count);
        }

        [Fact]
        public void should_round_trip_event_headers_through_the_wrapper()
        {
            _deserialized!.Headers["payload"].Should().Be("large");
        }
    }

    internal sealed class ChunkedReadStream(Stream inner, int maxChunkSize) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => inner.Length;

        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override void Flush()
        {
            inner.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return inner.Read(buffer, offset, Math.Min(count, maxChunkSize));
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return inner.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            inner.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles
