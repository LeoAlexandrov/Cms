using System;
using System.IO;


namespace HCms.Infrastructure.Media
{
	public sealed class TempFileStream : Stream
	{
		readonly FileStream _stream;
		readonly string _tempPath;
		readonly string _filePath;
		bool _disposed;

		public TempFileStream(string tempPath, string fileName)
		{
			if (string.IsNullOrEmpty(tempPath))
				throw new ArgumentNullException(nameof(tempPath));

			if (string.IsNullOrEmpty(fileName))
				throw new ArgumentNullException(nameof(fileName));

			_tempPath = tempPath;
			_filePath = Path.Combine(tempPath, fileName);
			_stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
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
					_stream.Dispose();

					try
					{
						Directory.Delete(_tempPath, true);
					}
					catch
					{
					}
				}

				_disposed = true;
			}

			base.Dispose(disposing);
		}
	}
}
