using System;
using System.Collections.Generic;
using System.IO;

namespace VisualPinball.Engine.PinMAME
{
	public class PinMameAudioPipeStream : Stream
	{
		private readonly Queue<float> _buffer = new Queue<float>();
		private long _maxBufferLength = 8192;

		public long MaxBufferLength
		{
			get { return _maxBufferLength; }
			set { _maxBufferLength = value; }
		}

		public new void Dispose()
		{
			_buffer.Clear();
		}

		public override void Flush()
		{
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		public int Read(float[] buffer, int count)
		{
			if (buffer == null)
				throw new ArgumentException("Buffer is null");
			if (count > buffer.Length)
				throw new ArgumentException("The sum of offset and count is greater than the buffer length. ");
			if (count < 0)
				throw new ArgumentOutOfRangeException("offset", "offset or count is negative.");

			if (count == 0)
				return 0;

			int readLength = 0;

			lock (_buffer)
			{
				for (; readLength < count && Length > 0; readLength++)
				{
					buffer[readLength] = _buffer.Dequeue();
				}
			}

			return readLength;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}

		private bool ReadAvailable(int count)
		{
			return (Length >= count);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}

		public void Write(IntPtr buffer, int count)
		{
			if (buffer == null)
				throw new ArgumentException("Buffer is null");
			if (count < 0)
				throw new ArgumentOutOfRangeException("offset", "offset or count is negative.");
			if (count == 0)
				return;

			lock (_buffer)
			{
				while (Length >= _maxBufferLength)
					return;

				unsafe
				{
					var src = (float*)buffer;
					for (var index = 0; index < count; index++)
					{
						_buffer.Enqueue(src[index]);
					}
				}
			}
		}

		public override bool CanRead
		{
			get { return true; }
		}

		public override bool CanSeek
		{
			get { return false; }
		}

		public override bool CanWrite
		{
			get { return true; }
		}

		public override long Length
		{
			get { return _buffer.Count; }
		}

		public override long Position
		{
			get { return 0; }
			set { throw new NotImplementedException(); }
		}
	}
}