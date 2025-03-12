using System;
using Microsoft.Extensions.Configuration;


namespace HCms.Routing
{
	/// <summary>
	/// Represents a path transformer whose task is to transform CMS logical path to the URL path and vice versa. 
	/// </summary>
	public interface IPathTransformer
	{
		/// <summary>
		/// Transforms logical path of the CMS object (document or media content item) to the URL path.
		/// </summary>
		/// <param name="root">Slug of the root document.</param>
		/// <param name="path">Document or media content item path.</param>
		/// <param name="isMedia">true for media content, false for documents. </param>
		/// <returns>URL path of the object.</returns>
		string Forward(string root, string path, bool isMedia);

		/// <summary>
		/// Transforms URL path to the logical path of the CMS document.
		/// </summary>
		/// <param name="host">Host part of the URL</param>
		/// <param name="path">Path part of the URL.</param>
		/// <returns>A tuple with the slug of the root document and logical path of the CMS document.</returns>
		(string, string) Back(string host, string path);
	}



	/// <summary>
	/// Default path transformer for single site CMS configurations. Transforms CMS logical path to the URL path and vice versa. 
	/// </summary>
	public class DefaultPathTransformer(IConfiguration configuration) : IPathTransformer
	{
		readonly Uri mediaHost = new(configuration["MediaHost"] ?? "/");

		public string Forward(string root, string path, bool isMedia)
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