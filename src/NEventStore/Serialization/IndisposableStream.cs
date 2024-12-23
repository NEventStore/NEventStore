namespace NEventStore.Serialization
{
    using System.IO;

    /// <summary>
    ///    Represents a stream that wraps another stream and prevents it from being disposed.
    /// </summary>
    internal class IndisposableStream : Stream
    {
        private readonly Stream _stream;

        /// <summary>
        ///   Initializes a new instance of the IndisposableStream class.
        /// </summary>
        public IndisposableStream(Stream stream)
        {
            _stream = stream;
        }

        public override bool CanRead
        {
            get { return _stream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return _stream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _stream.CanWrite; }
        }

        public override long Length
        {
            get { return _stream.Length; }
        }

        public override long Position
        {
            get { return _stream.Position; }
            set { _stream.Position = value; }
        }

        protected override void Dispose(bool disposing)
        {
            // no-op
        }

        public override void Close()
        {
            // no-op
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }
    }
}