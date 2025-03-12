using System;
using System.Security.Cryptography;

namespace AleProjects.Random
{

	internal static class RandomString
	{
		const string allowedChars = "0123456789ABCDEFGHIJKLMNOPGRSTUVWXYZabcdefghijklmnopgrstuvwxyz";

		public static string Create(int len)
		{
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(len);

			Span<byte> bytes = stackalloc byte[len];
			RandomNumberGenerator.Fill(bytes);

			for (int i = 0; i < bytes.Length; i++)
				bytes[i] = (byte)allowedChars[bytes[i] % allowedChars.Length];

			return System.Text.Encoding.ASCII.GetString(bytes);
		}
	}
}