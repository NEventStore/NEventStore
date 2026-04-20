using FluentAssertions;
using NEventStore.Persistence.AcceptanceTests.BDD;
using NEventStore.Serialization;
#if MSTEST
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
#if XUNIT
using Xunit;
using Xunit.Should;
#endif

#pragma warning disable IDE1006 // Naming Styles

namespace NEventStore
{
#if MSTEST
    [TestClass]
#endif
    public class when_serializing_with_a_serializer_that_disposes_the_output_stream
    {
        private readonly byte[] _payload = [1, 2, 3, 4, 5];

        [Fact]
        public void should_return_the_serialized_bytes()
        {
            var serialized = new DisposingSerializer(_payload).Serialize(new object());

            serialized.Should().Equal(_payload);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_serializing_a_large_payload_written_in_many_chunks
    {
        private readonly byte[] _payload = Enumerable.Range(0, 128 * 1024)
            .Select(i => (byte)(i % 251))
            .ToArray();

        [Fact]
        public void should_return_an_exact_byte_array()
        {
            var serialized = new ChunkedSerializer(_payload, chunkSize: 37).Serialize(new object());

            serialized.Should().Equal(_payload);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_serializing_and_inspecting_the_output_stream
    {
        [Fact]
        public void should_use_the_target_specific_output_stream()
        {
            var serializer = new InspectingSerializer();

            serializer.Serialize(new object());

#if NET8_0_OR_GREATER
            serializer.OutputStreamTypeName.Should().Be("PooledWriteStream");
            serializer.OutputCanRead.Should().BeFalse();
            serializer.OutputCanSeek.Should().BeFalse();
#else
            serializer.OutputStreamTypeName.Should().Be(nameof(MemoryStream));
            serializer.OutputCanRead.Should().BeTrue();
            serializer.OutputCanSeek.Should().BeTrue();
#endif
            serializer.OutputCanWrite.Should().BeTrue();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_serializing_one_byte_at_a_time
    {
        private readonly byte[] _payload = Enumerable.Range(0, 1024)
            .Select(i => (byte)(i % 251))
            .ToArray();

        [Fact]
        public void should_return_an_exact_byte_array()
        {
            var serialized = new ByteByByteSerializer(_payload).Serialize(new object());

            serialized.Should().Equal(_payload);
        }
    }

    internal sealed class DisposingSerializer(byte[] payload) : ISerialize
    {
        public void Serialize<T>(Stream output, T graph) where T : notnull
        {
            output.Write(payload, 0, payload.Length);
            // Several production serializers dispose wrapper writers that close the supplied
            // stream. The extension owns the final byte[] materialization, so this behavior must
            // stay safe for both MemoryStream and the modern pooled stream implementation.
            output.Dispose();
        }

        public T? Deserialize<T>(Stream input)
        {
            throw new NotSupportedException();
        }
    }

    internal sealed class InspectingSerializer : ISerialize
    {
        public string? OutputStreamTypeName { get; private set; }

        public bool OutputCanRead { get; private set; }

        public bool OutputCanSeek { get; private set; }

        public bool OutputCanWrite { get; private set; }

        public void Serialize<T>(Stream output, T graph) where T : notnull
        {
            OutputStreamTypeName = output.GetType().Name;
            OutputCanRead = output.CanRead;
            OutputCanSeek = output.CanSeek;
            OutputCanWrite = output.CanWrite;
            output.WriteByte(42);
        }

        public T? Deserialize<T>(Stream input)
        {
            throw new NotSupportedException();
        }
    }

    internal sealed class ChunkedSerializer(byte[] payload, int chunkSize) : ISerialize
    {
        public void Serialize<T>(Stream output, T graph) where T : notnull
        {
            for (int offset = 0; offset < payload.Length; offset += chunkSize)
            {
                int count = Math.Min(chunkSize, payload.Length - offset);
                output.Write(payload, offset, count);
            }
        }

        public T? Deserialize<T>(Stream input)
        {
            throw new NotSupportedException();
        }
    }

    internal sealed class ByteByByteSerializer(byte[] payload) : ISerialize
    {
        public void Serialize<T>(Stream output, T graph) where T : notnull
        {
            foreach (byte value in payload)
            {
                output.WriteByte(value);
            }
        }

        public T? Deserialize<T>(Stream input)
        {
            throw new NotSupportedException();
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles
