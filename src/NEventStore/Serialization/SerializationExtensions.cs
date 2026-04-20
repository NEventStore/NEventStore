
#if NET8_0_OR_GREATER
using System.Buffers;
#endif

namespace NEventStore.Serialization
{
    /// <summary>
    ///     Implements extension methods that make call to the serialization infrastructure more simple.
    /// </summary>
    public static class SerializationExtensions
    {
        /// <summary>
        ///     Serializes the object provided.
        /// </summary>
        /// <typeparam name="T">The type of object to be serialized</typeparam>
        /// <param name="serializer">The serializer to use.</param>
        /// <param name="value">The object graph to be serialized.</param>
        /// <returns>A serialized representation of the object graph provided.</returns>
        public static byte[] Serialize<T>(this ISerialize serializer, T value) where T : notnull
        {
#if NET8_0_OR_GREATER
            // Modern package targets use a pooled backing buffer to avoid MemoryStream's
            // intermediate array allocation and growth copies on larger payloads. The public
            // contract still returns an exact byte[] so consumers see the same API shape and
            // ownership semantics as before; only temporary write storage is pooled.
            var stream = new PooledWriteStream();
            try
            {
                serializer.Serialize(stream, value);
                return stream.ToArray();
            }
            finally
            {
                // Some serializers dispose the stream they are given before returning. Dispose
                // is intentionally a no-op on PooledWriteStream so the extension can still copy
                // the final bytes and then deterministically return the rented buffer here.
                stream.ReturnBuffer();
            }
#else
            using var stream = new MemoryStream();
            serializer.Serialize(stream, value);
            return stream.ToArray();
#endif
        }

        /// <summary>
        ///     Deserializes the array of bytes provided.
        /// </summary>
        /// <typeparam name="T">The type of object to be deserialized.</typeparam>
        /// <param name="serializer">The serializer to use.</param>
        /// <param name="serialized">The serialized array of bytes.</param>
        /// <returns>The reconstituted object, if any.</returns>
        public static T? Deserialize<T>(this ISerialize serializer, byte[] serialized)
        {
            // add null or empty check
            if (serialized == null || serialized.Length == 0)
            {
                throw new ArgumentNullException(nameof(serialized), "cannot be null or empty.");
            }

            using var stream = new MemoryStream(serialized);
            return serializer.Deserialize<T>(stream);
        }

#if NET8_0_OR_GREATER
        private sealed class PooledWriteStream : Stream
        {
            private const int InitialCapacity = 256;
            private byte[] _buffer = ArrayPool<byte>.Shared.Rent(InitialCapacity);
            private int _length;
            private bool _bufferReturned;

            public override bool CanRead => false;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length => _length;

            public override long Position
            {
                get => _length;
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                ArgumentNullException.ThrowIfNull(buffer);
                if ((uint)offset > (uint)buffer.Length || (uint)count > (uint)(buffer.Length - offset))
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }

                Write(buffer.AsSpan(offset, count));
            }

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                EnsureCapacity(_length + buffer.Length);
                buffer.CopyTo(_buffer.AsSpan(_length));
                _length += buffer.Length;
            }

            public override void WriteByte(byte value)
            {
                EnsureCapacity(_length + 1);
                _buffer[_length] = value;
                _length++;
            }

            public byte[] ToArray()
            {
                var result = new byte[_length];
                _buffer.AsSpan(0, _length).CopyTo(result);
                return result;
            }

            public void ReturnBuffer()
            {
                if (_bufferReturned)
                {
                    return;
                }

                ArrayPool<byte>.Shared.Return(_buffer);
                _bufferReturned = true;
            }

            protected override void Dispose(bool disposing)
            {
                // Do not return the buffer from Dispose. Serializers commonly dispose the output
                // stream they receive, but Serialize<T>() still has to read back the bytes after
                // the serializer returns. The extension method owns the pooled buffer lifetime.
            }

            private void EnsureCapacity(int requiredCapacity)
            {
                if (requiredCapacity <= _buffer.Length)
                {
                    return;
                }

                int newCapacity = _buffer.Length;
                while (newCapacity < requiredCapacity)
                {
                    newCapacity *= 2;
                }

                var newBuffer = ArrayPool<byte>.Shared.Rent(newCapacity);
                _buffer.AsSpan(0, _length).CopyTo(newBuffer);
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = newBuffer;
            }
        }
#endif
    }
}
