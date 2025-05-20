using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using AleProjects.Base64;
using AleProjects.Cms.Domain.ValueObjects;


namespace AleProjects.Cms.Infrastructure.Media
{

	public class LocalMediaStorageSettings : BaseMediaStorageSettings
	{
		public const string DEFAULT_CACHE_FOLDER = ".cache";

		public string StoragePath { get; set; } = string.Empty;
		public string CacheFolder { get; set; } = DEFAULT_CACHE_FOLDER;
	}



	public class LocalMediaStorage(IOptions<LocalMediaStorageSettings> settings, ILogger<LocalMediaStorage> logger) : IMediaStorage
	{
		readonly LocalMediaStorageSettings _settings = settings.Value;
		readonly ILogger<LocalMediaStorage> _logger = logger;

		public BaseMediaStorageSettings Settings { get => _settings; }


		struct EntryNames(string fullName, string virtName)
		{
			public string FullName { get; set; } = fullName;
			public string VirtualName { get; set; } = virtName;
		}


		static string MimeType(string fileName)
		{
			var provider = new FileExtensionContentTypeProvider();

			if (provider.TryGetContentType(fileName, out string contentType))
				return contentType;

			return "application/octet-stream";
		}

		static string FromBase64ToVirtual(string previewName)
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

				if (!Base64Url.TryDecode(previewName[0..k], out result))
					result = string.Empty;
			}

			return result;
		}


		static bool DeletePreviews(string cachePath, IList<EntryNames> files, IList<EntryNames> folders, ILogger<LocalMediaStorage> logger)
		{
			var cached = Directory.GetFiles(cachePath);

			int l = cachePath.Length;

			if (!cachePath.EndsWith(Path.DirectorySeparatorChar))
				l++;

			var previews = cached.Select(c => new EntryNames(c, FromBase64ToVirtual(c[l..]))).ToArray();

			bool result = true;

			foreach (var preview in previews)
			{
				foreach (var file in files)
					if (preview.VirtualName.StartsWith(file.VirtualName) &&
						preview.VirtualName.Length > file.VirtualName.Length &&
						preview.VirtualName[file.VirtualName.Length] == '_')
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
					if (preview.VirtualName.StartsWith(folder.VirtualName) &&
						preview.VirtualName.Length > folder.VirtualName.Length &&
						preview.VirtualName[folder.VirtualName.Length] == Path.DirectorySeparatorChar)
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

		static bool CheckSignature(byte[] header, string extension)
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

					string s = UTF8Encoding.UTF8.GetString(header, 0, len);

					return s.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>") || s.StartsWith("<svg ");

				case ".bmp":
					return header.Length > 2 && header[0] == 'B' && header[1] == 'M';

				case ".gif":
					return header.Length > 5 && header[0] == 'G' && header[1] == 'I' && header[2] == 'F' && header[3] == '8' &&
						(header[4] == '7' || header[4] == '9');

				case ".tif":
				case ".tiff":
					return header.Length > 4 &&
						((header[0] == 0x49 && header[1] == 0x49 && header[2] == 0x2a && header[3] == 0) ||
						 (header[0] == 0x4d && header[1] == 0x4d && header[2] == 0 && header[3] == 0x2a));

				case "webm":
					return header.Length > 4 && header[0] == 0x1a && header[1] == 0x45 && header[2] == 0xdf && header[3] == 0xa3;

				case ".mp4":
					return header.Length > 8 &&
						((header[0] == 'f' && header[1] == 't' && header[2] == 'y' && header[3] == 'p' && header[4] == 'i' && header[5] == 's' && header[6] == 'o' && header[7] == 'm') ||
						 (header[0] == 'f' && header[1] == 't' && header[2] == 'y' && header[3] == 'p' && header[4] == 'M' && header[5] == 'S' && header[6] == 'N' && header[7] == 'V'));

				case ".mpg":
				case ".mpeg":
					return header.Length > 4 && header[0] == 0x00 && header[1] == 0x00 && header[2] == 0x01 && (header[3] == 0xb3 || header[3] == 0xba);

				default:
					return true;
			}
		}


		public List<MediaStorageEntry> ReadDirectory(string path)
		{
			string storagePath = _settings.StoragePath;
			string fullPath = Path.Combine(storagePath, path ?? "");
			int storagePathLen = storagePath.Length;

			if (!storagePath.EndsWith(Path.DirectorySeparatorChar))
				storagePathLen++;

			if (!Directory.Exists(fullPath))
				return null;

			var di = new DirectoryInfo(fullPath);
			var folders = di.GetDirectories();
			var files = di.GetFiles();

			List<MediaStorageEntry> result = new(folders.Length + files.Length);

			for (int i = 0; i < folders.Length; i++)
				if ((folders[i].Attributes & FileAttributes.Hidden) == 0)
					result.Add(
						new MediaStorageEntry()
						{
							IsFolder = true,
							Name = folders[i].Name,
							FullName = folders[i].FullName,
							RelativeName = folders[i].FullName[storagePathLen..],
							Extension = folders[i].Extension,
							Date = folders[i].CreationTime
						}
					);

			result.Sort();

			int sortStartIdx = result.Count;

			for (int i = 0; i < files.Length; i++)
				if ((files[i].Attributes & FileAttributes.Hidden) == 0)
					result.Add(
						new MediaStorageEntry()
						{
							Name = files[i].Name,
							FullName = files[i].FullName,
							RelativeName = files[i].FullName[storagePathLen..],
							Extension = files[i].Extension,
							Size = files[i].Length,
							Date = files[i].CreationTime,
							MimeType = MimeType(files[i].Name)
						}
					);

			result.Sort(sortStartIdx, result.Count - sortStartIdx, null);

			return result;
		}

		public MediaStorageEntry GetFile(string path)
		{
			string storagePath = _settings.StoragePath;
			string fullPath = Path.Combine(storagePath, path ?? "");

			if (!File.Exists(fullPath))
				return null;

			FileInfo fi = new(fullPath);

			MediaStorageEntry result = new()
			{
				Name = fi.Name,
				FullName = fi.FullName,
				RelativeName = path,
				Extension = fi.Extension,
				Size = fi.Length,
				Date = fi.CreationTime,
				MimeType = MimeType(fi.Name)
			};

			return result;
		}

		public async ValueTask<MediaStorageEntry> Preview(string path, string previewPrefix, int size)
		{
			string storagePath = _settings.StoragePath;
			string fullPath;

			if (Path.GetExtension(path) == ".svg")
			{
				// no need in preview for svg-files

				fullPath = Path.Combine(storagePath, path ?? "");

				if (!File.Exists(fullPath))
					return null;

				FileInfo fi = new(fullPath);

				return new MediaStorageEntry()
				{
					Name = fi.Name,
					FullName = fi.FullName,
					RelativeName = path,
					Extension = fi.Extension,
					Size = fi.Length,
					Date = fi.CreationTime,
					MimeType = MimeType(fi.Name)
				};
			}

			string cacheFolder = _settings.CacheFolder;
			string cachedName = $"{System.Web.HttpUtility.UrlEncode(previewPrefix)}_{size}x{size}.webp";
			string cachedPath = Path.Combine(storagePath, cacheFolder, cachedName);

			if (File.Exists(cachedPath))
			{
				// preview already exists

				FileInfo fi = new(cachedPath);

				return new MediaStorageEntry()
				{
					Name = fi.Name,
					FullName = fi.FullName,
					RelativeName = path,
					Extension = fi.Extension,
					Size = fi.Length,
					Date = fi.CreationTime,
					MimeType = MimeType(fi.Name)
				};
			}

			// generate preview and return it

			fullPath = Path.Combine(storagePath, path ?? "");

			if (!File.Exists(fullPath))
				return null;

			string extension = Path.GetExtension(fullPath).ToLower();

			if (extension == ".webp" || extension == ".png" || extension == ".jpg" ||
				extension == ".gif" || extension == ".bmp" || extension == ".tif" || extension == ".tiff")
			{
				using var stream = File.OpenRead(fullPath);
				using var output = File.OpenWrite(cachedPath);

				var image = await Image.LoadAsync<Rgba32>(stream);

				if (image.Height > size || image.Width > size)
					image.Mutate(x => x.Resize(new ResizeOptions() { Mode = ResizeMode.Pad, Size = new(size, size), PadColor = Color.Transparent }));
				else
					image.Mutate(x => x.Pad(size, size, Color.Transparent));

				await image.SaveAsWebpAsync(output);
			}
			else
			{
				using var image = new Image<Rgba32>(size, size);
				using var output = File.OpenWrite(cachedPath);

				image.Mutate(x => x.BackgroundColor(Color.Gainsboro));

				await image.SaveAsWebpAsync(output);
			}

			FileInfo f = new(cachedPath);

			return new MediaStorageEntry()
			{
				Name = f.Name,
				FullName = f.FullName,
				RelativeName = path,
				Extension = f.Extension,
				Size = f.Length,
				Date = f.CreationTime,
				MimeType = MimeType(f.Name)
			};
		}

		public async Task<MediaStorageEntry> Properties(string path)
		{
			var result = GetFile(path);

			if (result == null)
				return null;

			string extension = Path.GetExtension(path).ToLower();

			if (extension == ".webp" || extension == ".png" || extension == ".jpg" ||
				extension == ".gif" || extension == ".bmp" || extension == ".tif" || extension == ".tiff")
			{
				string storagePath = _settings.StoragePath;
				string fullPath = Path.Combine(storagePath, path ?? "");

				using var stream = File.OpenRead(fullPath);

				var image = await Image.LoadAsync(stream);

				result.Width = image.Width;
				result.Height = image.Height;
			}


			return result;
		}

		public async Task<MediaStorageEntry> Save(Stream stream, string fileName, string destination)
		{
			string storagePath = _settings.StoragePath;
			string relativeName = Path.Combine(destination ?? "", fileName);
			string fullPath = Path.Combine(storagePath, relativeName);
			string extension = Path.GetExtension(fileName);

			byte[] buf = new byte[1024 * 1024];
			long totalRead = 0;
			int read = -1;

			using var fileStream = File.Create(fullPath);

			while (read != 0)
			{
				read = await stream.ReadAsync(buf);

				if (totalRead == 0 && !CheckSignature(buf, extension))
					return null;

				totalRead += read;

				if (read > 0)
					await fileStream.WriteAsync(buf.AsMemory(0, read));
			}

			MediaStorageEntry result = new()
			{
				Name = fileName,
				FullName = fullPath,
				RelativeName = relativeName,
				Extension = extension,
				Size = totalRead,
				Date = DateTime.Now,
				MimeType = MimeType(fileName)
			};

			return result;
		}

		public async Task<string[]> Delete(string[] entries)
		{
			string storagePath = _settings.StoragePath;
			string cacheFolder = _settings.CacheFolder;
			string cachePath = Path.Combine(storagePath, cacheFolder);

			List<EntryNames> files = [];
			List<EntryNames> folders = [];

			foreach (string entry in entries)
			{
				var name = Path.Combine(storagePath, entry);
				var fi = new FileInfo(name);

				if ((fi.Attributes & FileAttributes.Directory) != 0)
					folders.Add(new(name, entry));
				else
					files.Add(new(name, entry));
			}

			ConcurrentBag<string> list = [];

			Task task1 = Task.Run(() =>
			{
				foreach (var folder in folders)
					try
					{
						Directory.Delete(folder.FullName, true);
						list.Add(folder.VirtualName);
					}
					catch (Exception ex)
					{
						_logger?.LogWarning(ex, "Error deleting folder {FullName}", folder.FullName);
					}
			});

			Task task2 = Task.Run(() =>
			{
				foreach (var file in files)
					try
					{
						File.Delete(file.FullName);
						list.Add(file.VirtualName);
					}
					catch (Exception ex)
					{
						_logger?.LogWarning(ex, "Error deleting file {FullName}", file.FullName);
					}
			});

			Task task3 = Task.Run(() =>
			{
				DeletePreviews(cachePath, files, folders, _logger);
			});

			Task[] tasks = [task1, task2, task3];

			await Task.WhenAll(tasks);

			var result = list.ToArray();

			Array.Sort(result);

			return result;
		}

		public MediaStorageEntry CreateFolder(string name, string path)
		{
			string storagePath = _settings.StoragePath;
			string fullPath = Path.Combine(storagePath, path ?? "", name);

			MediaStorageEntry result;

			try
			{
				Directory.CreateDirectory(fullPath);

				int k = name.IndexOf(Path.DirectorySeparatorChar);

				if (k < 0)
					k = name.Length;

				var fi = new FileInfo(Path.Combine(storagePath, path ?? "", name[0..k]));

				result = new()
				{
					IsFolder = true,
					FullName = fi.FullName,
					Name = fi.Name,
					Extension = fi.Extension,
					Date = fi.CreationTime,
					RelativeName = Path.Combine(path ?? "", fi.Name)
				};
			}
			catch (Exception ex)
			{
				_logger?.LogError(ex, "Error creating folder {FullPath}", fullPath);
				result = null;
			}

			return result;
		}

		public static void CheckAndCreateCacheFolder(IConfiguration configuration)
		{
			var settings = configuration.GetSection("Media").Get<LocalMediaStorageSettings>();

			string storagePath = settings.StoragePath;
			string cacheFolder = settings.CacheFolder;
			//string storagePath = configuration.GetValue<string>("Media:StoragePath");
			//string cacheFolder = configuration.GetValue<string>("Media:CacheFolder", LocalMediaStorageSettings.DEFAULT_CACHE_FOLDER);
			string cachePath = Path.Combine(storagePath, cacheFolder);

			if (!Directory.Exists(cachePath))
				Directory.CreateDirectory(cachePath);

			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
				File.SetAttributes(cachePath, FileAttributes.Hidden);
		}
	}

}