using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using AleProjects.Base64;
using HCms.Domain.Types;


namespace HCms.Infrastructure.Media
{

	public interface IMediaStorage
	{
		string[] PlaceKeys { get; }
		bool ServesPath(string path);
		CommonMediaStorageParams GetCommonParams(string path);
		Task<List<MediaStorageEntry>> ReadDirectory(string path);
		Task<MediaStorageEntry> GetFile(string path);
		ValueTask<MediaStorageEntry> Preview(string path, string previewPrefix, int size);
		Task<MediaStorageEntry> Properties(string path);
		Task<MediaStorageEntry> Save(Stream stream, string fileName, string destination);
		Task<string[]> Delete(string[] entries);
		Task<MediaStorageEntry> CreateFolder(string name, string path);
	}



	public class BaseMediaStorage
	{
		protected const string DEFAULT_CACHE_FOLDER = "/var/tmp/hcms";

		protected readonly PlatformID _platformID = Environment.OSVersion.Platform;

		protected struct EntryNames(string fullName, string osNeutralName)
		{
			public string FullName { get; set; } = fullName;
			public string OsNeutralName { get; set; } = osNeutralName;
		}

		protected string ToOSPath(string path)
		{
			if (string.IsNullOrEmpty(path))
				return string.Empty;

			if (_platformID == PlatformID.Win32NT)
				return path.Replace('/', Path.DirectorySeparatorChar);

			return path;
		}

		protected string ToUnixPath(string path)
		{
			if (string.IsNullOrEmpty(path))
				return string.Empty;

			if (_platformID == PlatformID.Win32NT)
				return path.Replace(Path.DirectorySeparatorChar, '/');

			return path;
		}


		protected static string MimeType(string fileName)
		{
			var provider = new FileExtensionContentTypeProvider();

			if (provider.TryGetContentType(fileName, out string contentType))
				return contentType;

			return "application/octet-stream";
		}

		protected static string FromBase64ToNeutral(string previewName)
		{
			string result;

			if (string.IsNullOrEmpty(previewName))
			{
				result = string.Empty;
			}
			else
			{
				int k = previewName.LastIndexOf('_');

				if (k < 0)
					k = previewName.Length;

				if (!Base64Url.TryDecode(previewName.AsSpan()[0..k], out result))
					result = string.Empty;
			}

			return result;
		}

		protected static bool DeletePreviews<T>(string cachePath, IEnumerable<string> files, IEnumerable<string> folders, ILogger<T> logger)
			where T : BaseMediaStorage
		{
			var cached = Directory.GetFiles(cachePath);

			int l = cachePath.Length;

			if (!cachePath.EndsWith(Path.DirectorySeparatorChar))
				l++;

			var previews = cached.Select(c => new EntryNames(c, FromBase64ToNeutral(c[l..]))).ToArray();

			bool result = true;

			foreach (var preview in previews)
			{
				foreach (var file in files)
					if (preview.OsNeutralName == file)
					{
						try
						{
							File.Delete(preview.FullName);
						}
						catch (Exception ex)
						{
							logger?.LogWarning(ex, "Error deleting preview file {FullName}", preview.FullName);
							result = false;
						}
					}

				foreach (var folder in folders)
					if (preview.OsNeutralName.StartsWith(folder) &&
						preview.OsNeutralName.Length > folder.Length &&
						preview.OsNeutralName[folder.Length] == '/')
					{
						try
						{
							File.Delete(preview.FullName);
						}
						catch (Exception ex)
						{
							logger?.LogWarning(ex, "Error deleting preview file {FullName}", preview.FullName);
							result = false;
						}
					}
			}

			return result;
		}

		protected static bool CheckSignature(byte[] header, string extension)
		{
			switch (extension)
			{
				case ".png":
					return header.Length > 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4e && header[3] == 0x47 &&
						header[4] == 0x0d && header[5] == 0x0a && header[6] == 0x1a && header[7] == 0x0a;

				case ".jpg":
					return header.Length > 4 && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff &&
						(header[3] == 0xe0 || header[3] == 0xe1 || header[3] == 0xe2 || header[3] == 0xe3);

				case ".webp":
				case ".avi":
					return header.Length > 4 && header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F';

				case ".svg":

					int len = 48;

					if (len > header.Length)
						len = header.Length;

					string s = Encoding.UTF8.GetString(header, 0, len);

					return s.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>") || s.StartsWith("<svg ");

				case ".bmp":
					return header.Length > 2 && header[0] == 'B' && header[1] == 'M';

				case ".gif":
					return header.Length > 5 && header[0] == 'G' && header[1] == 'I' && header[2] == 'F' && header[3] == '8' &&
						(header[4] == '7' || header[4] == '9');

				case ".tif":
				case ".tiff":
					return header.Length > 4 &&
						(header[0] == 0x49 && header[1] == 0x49 && header[2] == 0x2a && header[3] == 0 ||
						 header[0] == 0x4d && header[1] == 0x4d && header[2] == 0 && header[3] == 0x2a);

				case "webm":
					return header.Length > 4 && header[0] == 0x1a && header[1] == 0x45 && header[2] == 0xdf && header[3] == 0xa3;

				case ".mp4":
					return header.Length > 8 &&
						(header[0] == 'f' && header[1] == 't' && header[2] == 'y' && header[3] == 'p' && header[4] == 'i' && header[5] == 's' && header[6] == 'o' && header[7] == 'm' ||
						 header[0] == 'f' && header[1] == 't' && header[2] == 'y' && header[3] == 'p' && header[4] == 'M' && header[5] == 'S' && header[6] == 'N' && header[7] == 'V');

				case ".mpg":
				case ".mpeg":
					return header.Length > 4 && header[0] == 0x00 && header[1] == 0x00 && header[2] == 0x01 && (header[3] == 0xb3 || header[3] == 0xba);

				default:
					return true;
			}
		}

		public static void CheckAndCreateCacheFolder(IConfiguration configuration)
		{
			var settings = configuration.GetSection("Media").Get<BaseMediaStorageSettings>();

			string cacheFolder = settings.CacheFolder;

			if (!Directory.Exists(cacheFolder))
				Directory.CreateDirectory(cacheFolder);

			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
				File.SetAttributes(cacheFolder, FileAttributes.Hidden);
		}

	}

}
