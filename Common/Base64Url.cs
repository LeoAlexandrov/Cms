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

				Span<char> chars = stackalloc char[l + padding * 2];

				Convert.TryToBase64Chars(bytes, chars, out l);

				for (int i = 0; i < l; i++)
					if (chars[i] == '+')
						chars[i] = '-';
					else if (chars[i] == '/')
						chars[i] = '_';

				l -= padding;

				for (int i = 0; i < padding; i++)
				{
					chars[l++] = '%';
					chars[l++] = '3';
					chars[l++] = 'd';
				}

				return new string(chars);
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

				l +=  l % 4;

				char[] chars = new char[l + padding * 2];

				Convert.TryToBase64Chars(bytes.AsSpan(), chars, out l);

				for (int i = 0; i < l; i++)
					if (chars[i] == '+')
						chars[i] = '-';
					else if (chars[i] == '/')
						chars[i] = '_';

				l -= padding;

				for (int i = 0; i < padding; i++)
				{
					chars[l++] = '%';
					chars[l++] = '3';
					chars[l++] = 'd';
				}

				return new string(chars);
			}
		}

		
		public static bool TryDecode(string base64url, out string result)
		{
			if (string.IsNullOrEmpty(base64url))
			{
				result = base64url;
				return true;
			}

			int n = base64url.Length;

			if (n <= STACKALLOC_THRESHOLD)
			{
				Span<char> chars = stackalloc char[n];
				Span<byte> bytes = stackalloc byte[n];

				base64url.CopyTo(chars);

				for (int i = 0; i < chars.Length; i++)
					if (chars[i] == '-')
						chars[i] = '+';
					else if (chars[i] == '_')
						chars[i] = '/';

				int len;

				if (base64url.EndsWith("%3d%3d"))
				{
					len = chars.Length - 4;
					chars[len - 2] = '=';
					chars[len - 1] = '=';
				}
				else if (base64url.EndsWith("%3d"))
				{
					len = chars.Length - 2;
					chars[len - 1] = '=';
				}
				else
					len = chars.Length;

				if (!Convert.TryFromBase64Chars(chars[..len], bytes, out int l))
				{
					result = null;

					return false;
				}

				result = Encoding.UTF8.GetString(bytes[..l]);
			}
			else
			{
				char[] chars = base64url.ToCharArray();
				byte[] bytes = new byte[n];

				for (int i = 0; i < chars.Length; i++)
					if (chars[i] == '-')
						chars[i] = '+';
					else if (chars[i] == '_')
						chars[i] = '/';

				int len;

				if (base64url.EndsWith("%3d%3d"))
				{
					len = chars.Length - 4;
					chars[len - 2] = '=';
					chars[len - 1] = '=';
				}
				else if (base64url.EndsWith("%3d"))
				{
					len = chars.Length - 2;
					chars[len - 1] = '=';
				}
				else
					len = chars.Length;

				if (!Convert.TryFromBase64Chars(chars.AsSpan(0, len), bytes, out int l))
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
