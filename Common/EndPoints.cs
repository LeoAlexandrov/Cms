using System;
using System.Collections.Generic;
using System.Net;


namespace AleProjects.Endpoints
{
	internal static class EndPoints
	{
		public static bool TryParse(string endpointString, out EndPoint endPoint)
		{
			int k = endpointString.LastIndexOf(':');

			if (k <= 0 || k == endpointString.Length - 1)
			{
				endPoint = null;
				return false;
			}

			if (!int.TryParse(endpointString[(k + 1)..], out int port) || port <= 0 || port > ushort.MaxValue)
			{
				endPoint = null;
				return false;
			}

			if (IPAddress.TryParse(endpointString[0..k], out IPAddress ip))
			{
				endPoint = new IPEndPoint(ip, port);
				return true;
			}

			try
			{
				endPoint = new DnsEndPoint(endpointString[0..k], port);
			}
			catch
			{
				endPoint = null;
			}

			return endPoint != null;
		}

		public static List<EndPoint> Parse(string endpointString, char separator = ' ')
		{
			var parts = endpointString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
			var result = new List<EndPoint>();

			foreach (var part in parts)
				if (TryParse(part, out EndPoint endPoint))
					result.Add(endPoint);

			return result;
		}

	}

}