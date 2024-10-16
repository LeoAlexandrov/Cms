using System;
using System.Security.Cryptography;

namespace AleProjects.Cms
{

	public static class RandomString
	{
		const string allowedChars = "0123456789ABCDEFGHIJKLMNOPGRSTUVWXYZabcdefghijklmnopgrstuvwxyz";

		public static string Create(int len)
		{
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(len);

			byte[] bytes = RandomNumberGenerator.GetBytes(len);

			for (int i = 0; i < bytes.Length; i++)
				bytes[i] = (byte)allowedChars[bytes[i] % allowedChars.Length];

			return System.Text.Encoding.ASCII.GetString(bytes);
		}
	}
}