using System;
using Microsoft.Extensions.Configuration;


namespace HCms.Routing
{

	public interface IPathTransformer
	{
		string Forward(string path, bool isMedia, string root);
		(string, string) Back(string host, string path);
	}



	public class DefaultPathTransformer(IConfiguration configuration) : IPathTransformer
	{
		readonly Uri mediaHost = new(configuration["MediaHost"] ?? "/");

		public string Forward(string path, bool isMedia, string root)
		{
			if (isMedia && 
				string.Compare(path, 0, "https://", 0, "https://".Length, StringComparison.OrdinalIgnoreCase) != 0 &&
				string.Compare(path, 0, "http://", 0, "http://".Length, StringComparison.OrdinalIgnoreCase) != 0)
			{
				var mediaUri = new Uri(mediaHost, path);
				return mediaUri.ToString();
			}

			return path;
		}

		public (string, string) Back(string host, string path)
		{
			return (null, path);
		}
	}
}