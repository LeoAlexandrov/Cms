using System;
using Microsoft.Extensions.Configuration;

namespace AleProjects.Cms.Sdk.ContentRepo
{

	public interface IReferenceTransformer
	{
		string Forward(string path, bool isMedia, string root);
		(string, string) Back(string path);
	}



	public class DefaultReferenceTransformer(IConfiguration configuration) : IReferenceTransformer
	{
		readonly Uri mediaHost = new(configuration["MediaHost"] ?? "/");

		public string Forward(string path, bool isMedia, string root)
		{
			if (isMedia)
			{
				var mediaUri = new Uri(mediaHost, path);
				return mediaUri.ToString();
			}

			return path;
		}

		public (string, string) Back(string path)
		{
			// just a stub
			return (path, null);
		}
	}
}