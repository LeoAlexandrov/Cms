using System;
using System.Text;

namespace AleProjects.Hashing.MurmurHash3
{

	internal static class MurmurHash3
	{
		private const int STACKALLOC_THRESHOLD = 256;

		public static uint Hash32(ReadOnlySpan<byte> data, uint seed = 0)
		{
			const uint c1 = 0xcc9e2d51;
			const uint c2 = 0x1b873593;
			const int r1 = 15;
			const int r2 = 13;
			const uint m = 5;
			const uint n = 0xe6546b64;

			uint hash = seed;
			int length = data.Length;
			int blockCount = length / 4;

			// Process 4-byte blocks
			for (int i = 0; i < blockCount; i++)
			{
				uint k = BitConverter.ToUInt32(data[(i * 4)..]);
				k *= c1;
				k = (k << r1) | (k >> (32 - r1));
				k *= c2;

				hash ^= k;
				hash = (hash << r2) | (hash >> (32 - r2));
				hash = hash * m + n;
			}

			// Process remaining bytes
			uint tail = 0;
			int remainingBytes = length & 3;
			if (remainingBytes > 0)
			{
				for (int i = remainingBytes - 1; i >= 0; i--)
				{
					tail <<= 8;
					tail |= data[length - remainingBytes + i];
				}
				tail *= c1;
				tail = (tail << r1) | (tail >> (32 - r1));
				tail *= c2;
				hash ^= tail;
			}

			// Finalization
			hash ^= (uint)length;
			hash ^= (hash >> 16);
			hash *= 0x85ebca6b;
			hash ^= (hash >> 13);
			hash *= 0xc2b2ae35;
			hash ^= (hash >> 16);

			return hash;
		}


		public static uint Hash32(string data, uint seed = 0)
		{
			int n = Encoding.UTF8.GetByteCount(data);

			if (n <= STACKALLOC_THRESHOLD)
			{
				Span<byte> bytes = stackalloc byte[n];

				Encoding.UTF8.GetBytes(data.AsSpan(), bytes);

				return Hash32(bytes, seed);
			}
			else
			{
				var bytes = Encoding.UTF8.GetBytes(data);

				return Hash32(bytes.AsSpan(), seed);
			}

		}
	}
}