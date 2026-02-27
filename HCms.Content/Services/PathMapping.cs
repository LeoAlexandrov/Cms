using System;


namespace HCms.Content.Services
{
	/// <summary>
	/// Represents a path mapper whose task is to map CMS logical path to the URL path. 
	/// </summary>
	public interface IPathMapper
	{
		/// <summary>
		/// Maps logical path of the CMS object (document or media content item) to the URL path.
		/// </summary>
		/// <param name="root">Slug of the root document. Null for media item.</param>
		/// <param name="path">Document or media content item path.</param>
		/// <param name="media">true for media content, false for documents. </param>
		/// <returns>URL path of the object.</returns>
		string Map(string root, string path, bool media = false);
	}



	/// <summary>
	/// Represents a factory of path mappers. Assumed to be injected by DI container.
	/// </summary>
	public interface IPathMapperFactory
	{
		/// <summary>
		/// Returns a path mapper by its name.
		/// </summary>
		/// <param name="name">Name of the path mapper.</param>
		/// <returns>Path mapper</returns>
		IPathMapper Get(string name);
	}



	/// <summary>
	/// Default path mapper for single site and single media storarage CMS configurations.
	/// Maps CMS logical path to the URL path. 
	/// </summary>
	public class DefaultPathMapper : IPathMapper
	{
		const string HTTPS = "https://";
		const string HTTP = "http://";

		readonly Uri baseMediaHost;

		public DefaultPathMapper(string mediaHost)
		{
			string host = mediaHost;

			if (string.IsNullOrEmpty(host))
				host = "/";
			else if (host[^1] != '/')
				host += "/";

			baseMediaHost = new Uri(host);
		}

		public string Map(string root, string path, bool media)
		{
			if (media &&
				string.Compare(path, 0, HTTPS, 0, HTTPS.Length, StringComparison.OrdinalIgnoreCase) != 0 &&
				string.Compare(path, 0, HTTP, 0, HTTP.Length, StringComparison.OrdinalIgnoreCase) != 0)
			{
				int i = path.IndexOf('/'); // there is the key for media storage place or s3 bucket before the first '/'
				var mediaUri = new Uri(baseMediaHost, path[(i + 1)..]);
				return mediaUri.ToString();
			}

			if (string.IsNullOrEmpty(path))
				return "/";

			return path;
		}
	}
}