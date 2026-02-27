using System;
using System.IO;

using Amazon.S3.Model;


namespace HCms.Infrastructure.Media
{
	public sealed class S3StreamWrapper : Stream
	{
		readonly GetObjectResponse _response;
		readonly Stream _stream;
		bool _disposed;

		public S3StreamWrapper(GetObjectResponse response)
		{
			ArgumentNullException.ThrowIfNull(response);

			_response = response;
			_stream = response.ResponseStream;
		}

		public override bool CanRead => _stream.CanRead;
		public override bool CanSeek => _stream.CanSeek;
		public override bool CanWrite => _stream.CanWrite;
		public override long Length => _stream.Length;
		public override long Position { get => _stream.Position; set => _stream.Position = value; }

		public override void Flush() => _stream.Flush();
		public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);
		public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
		public override void SetLength(long value) => _stream.SetLength(value);
		public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);

		protected override void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					_response.Dispose();
				}

				_disposed = true;
			}

			base.Dispose(disposing);
		}
	}
}
