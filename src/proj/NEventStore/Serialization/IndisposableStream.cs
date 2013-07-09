namespace EventStore.Serialization
{
    using System.IO;

    internal class IndisposableStream : Stream
	{
		private readonly Stream stream;

		public IndisposableStream(Stream stream)
		{
			this.stream = stream;
		}

		protected override void Dispose(bool disposing)
		{
			// no-op
		}
		public override void Close()
		{
			// no-op
		}

		public override bool CanRead
		{
			get { return this.stream.CanRead; }
		}
		public override bool CanSeek
		{
			get { return this.stream.CanSeek; }
		}
		public override bool CanWrite
		{
			get { return this.stream.CanWrite; }
		}
		public override long Length
		{
			get { return this.stream.Length; }
		}
		public override long Position
		{
			get { return this.stream.Position; }
			set { this.stream.Position = value; }
		}

		public override void Flush()
		{
			this.stream.Flush();
		}
		public override long Seek(long offset, SeekOrigin origin)
		{
			return this.stream.Seek(offset, origin);
		}
		public override void SetLength(long value)
		{
			this.stream.SetLength(value);
		}
		public override int Read(byte[] buffer, int offset, int count)
		{
			return this.stream.Read(buffer, offset, count);
		}
		public override void Write(byte[] buffer, int offset, int count)
		{
			this.stream.Write(buffer, offset, count);
		}
	}
}