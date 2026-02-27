using System;
using System.Text;


namespace AleProjects.Base64
{

	// https://datatracker.ietf.org/doc/html/rfc3548#section-4

	internal static class Base64Url
	{
		private const int STACKALLOC_THRESHOLD = 128;

		public static string Encode(string input)
		{
			int n = Encoding.UTF8.GetByteCount(input);

			if (n <= STACKALLOC_THRESHOLD)
			{
				Span<byte> bytes = stackalloc byte[n];

				n = Encoding.UTF8.GetBytes(input.AsSpan(), bytes);

				int l = n / 3;
				int padding = n % 3 != 0 ? (l + 1) * 3 - n : 0;

				n *= 4;
				l = n / 3;

				int diff = n - l * 3;

				if (diff > 0)
					l += diff;

				l += l % 4;

				Span<char> chars = stackalloc char[l];

				Convert.TryToBase64Chars(bytes, chars, out l);

				for (int i = 0; i < l; i++)
					if (chars[i] == '+')
						chars[i] = '-';
					else if (chars[i] == '/')
						chars[i] = '_';

				l -= padding;

				return new string(chars[..l]);
			}
			else
			{
				var bytes = Encoding.UTF8.GetBytes(input);

				int l = n / 3;
				int padding = n % 3 != 0 ? (l + 1) * 3 - n : 0;

				n *= 4;
				l = n / 3;

				int diff = n - l * 3;

				if (diff > 0)
					l += diff;

				l += l % 4;

				char[] chars = new char[l];

				Convert.TryToBase64Chars(bytes.AsSpan(), chars, out l);

				for (int i = 0; i < l; i++)
					if (chars[i] == '+')
						chars[i] = '-';
					else if (chars[i] == '/')
						chars[i] = '_';

				l -= padding;

				return new string(chars[..l]);
			}
		}

		public static bool TryDecode(ReadOnlySpan<char> base64url, out string result)
		{
			if (base64url.IsEmpty)
			{
				result = string.Empty;
				return true;
			}

			int n = base64url.Length;
			int hasPadding = 0;
			int rem4 = 0;

			if (base64url.EndsWith("%3d"))
			{
				if (base64url.EndsWith("%3d%3d"))
				{
					hasPadding = 2;
					n -= 4;
				}
				else
				{
					hasPadding = 1;
					n -= 2;
				}
			}
			else if (base64url[^1] != '=')
			{
				switch (rem4 = base64url.Length % 4)
				{
					case 2:
						n += 2;
						break;
					case 3:
						n += 1;
						break;
				}
			}


			if (n <= STACKALLOC_THRESHOLD)
			{
				Span<char> chars = stackalloc char[n];
				Span<byte> bytes = stackalloc byte[n];

				int len = base64url.Length - hasPadding * 3;

				for (int i = 0; i < len; i++)
					if (base64url[i] == '-')
						chars[i] = '+';
					else if (base64url[i] == '_')
						chars[i] = '/';
					else
						chars[i] = base64url[i];


				if (hasPadding == 2 || rem4 == 2)
				{
					chars[n - 2] = '=';
					chars[n - 1] = '=';
				}
				else if (hasPadding == 1 || rem4 == 3)
				{
					chars[n - 1] = '=';
				}

				if (!Convert.TryFromBase64Chars(chars, bytes, out int l))
				{
					result = null;

					return false;
				}

				result = Encoding.UTF8.GetString(bytes[..l]);
			}
			else
			{
				char[] chars = new char[n];
				byte[] bytes = new byte[n];

				int len = base64url.Length - hasPadding * 3;

				for (int i = 0; i < len; i++)
					if (base64url[i] == '-')
						chars[i] = '+';
					else if (base64url[i] == '_')
						chars[i] = '/';
					else
						chars[i] = base64url[i];


				if (hasPadding == 2 || rem4 == 2)
				{
					chars[n - 2] = '=';
					chars[n - 1] = '=';
				}
				else if (hasPadding == 1 || rem4 == 3)
				{
					chars[n - 1] = '=';
				}

				if (!Convert.TryFromBase64Chars(chars, bytes, out int l))
				{
					result = null;

					return false;
				}

				result = Encoding.UTF8.GetString(bytes[..l]);
			}

			return true;
		}

	}

}