using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCms.Domain.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using static HCms.Infrastructure.Media.S3MediaStorageSettings;


namespace HCms.Infrastructure.Media
{

	public class LocalMediaStorage : BaseMediaStorage, IMediaStorage
	{
		readonly LocalMediaStorageSettings _settings;
		readonly IFileIconProvider _fileIconProvider;
		readonly ILogger<LocalMediaStorage> _logger;


		public LocalMediaStorage(IOptions<LocalMediaStorageSettings> settings, IFileIconProvider fileIconProvider, ILogger<LocalMediaStorage> logger)
		{
			_settings = settings.Value;
			_fileIconProvider = fileIconProvider;
			_logger = logger;

			if (_settings.LocalDiskPlaces.Any(p => string.IsNullOrEmpty(p.Path) || string.IsNullOrEmpty(p.Key)))
				throw new ArgumentException("Some of bucket parameters (Path, Key) are null or empty.");

			if (_settings.LocalDiskPlaces.Any(p => p.Key.Contains('/')))
				throw new ArgumentException("Key cannot contain '/' character.");
		}

		(string, string, string) SplitPath(string path)
		{
			string key;
			string localPath;
			int i = path.IndexOf('/');

			if (i < 0)
			{
				key = path;
				localPath = string.Empty;
			}
			else
			{
				key = path[..i];
				localPath = path[(i + 1)..];
			}

			var place = _settings.LocalDiskPlaces.FirstOrDefault(b => b.Key == key);

			return (place?.Path ?? string.Empty, key, localPath);
		}

		public string[] PlaceKeys => [.. _settings.LocalDiskPlaces.Select(b => b.Key)];

		public bool ServesPath(string path)
		{
			var (_, key, _) = SplitPath(path);

			return _settings.LocalDiskPlaces.Any(p => p.Key == key);
		}

		public CommonMediaStorageParams GetCommonParams(string path)
		{
			var (_, key, _) = SplitPath(path);
			var place = _settings.LocalDiskPlaces.FirstOrDefault(p => p.Key == key);

			var result = new CommonMediaStorageParams()
			{
				MaxUploadSize = place?.MaxUploadSize ??_settings.MaxUploadSize,
				SafeNameRegex = place?.SafeNameRegex ??_settings.SafeNameRegex 
			};

			return result;
		}

		public Task<List<MediaStorageEntry>> ReadDirectory(string path)
		{
			List<MediaStorageEntry> result;

			if (string.IsNullOrEmpty(path))
			{
				result = [.. _settings.LocalDiskPlaces.Select(b => new MediaStorageEntry()
						{
							IsFolder = true,
							Name = b.Key,
							FullName = b.Key,
							RelativeName = b.Key,
							Extension = string.Empty,
							Date = DateTime.UnixEpoch,
						})];

				return Task.FromResult(result);
			}

			var (storagePath, key, objName) = SplitPath(path);

			string fullPath = Path.Combine(storagePath, ToOSPath(objName));
			int storagePathLen = storagePath.Length;

			if (!storagePath.EndsWith(Path.DirectorySeparatorChar))
				storagePathLen++;

			if (!Directory.Exists(fullPath))
				return Task.FromResult<List<MediaStorageEntry>>(null);

			var di = new DirectoryInfo(fullPath);
			var folders = di.GetDirectories();
			var files = di.GetFiles();

			result = new(folders.Length + files.Length);

			for (int i = 0; i < folders.Length; i++)
				if ((folders[i].Attributes & FileAttributes.Hidden) == 0)
					result.Add(
						new MediaStorageEntry()
						{
							IsFolder = true,
							Name = folders[i].Name,
							FullName = folders[i].FullName,
							RelativeName = key + "/" + ToUnixPath(folders[i].FullName[storagePathLen..]),
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
							RelativeName = key + "/" + ToUnixPath(files[i].FullName[storagePathLen..]),
							Extension = files[i].Extension,
							Size = files[i].Length,
							Date = files[i].CreationTime,
							MimeType = MimeType(files[i].Name)
						}
					);

			result.Sort(sortStartIdx, result.Count - sortStartIdx, null);

			return Task.FromResult(result);
		}

		public Task<MediaStorageEntry> GetFile(string path)
		{
			var (storagePath, key, objName) = SplitPath(path);

			string fullPath = Path.Combine(storagePath, ToOSPath(objName));

			if (!File.Exists(fullPath))
				return Task.FromResult<MediaStorageEntry>(null);

			FileInfo fi = new(fullPath);

			MediaStorageEntry result = new()
			{
				Name = fi.Name,
				FullName = fi.FullName,
				RelativeName = key + "/" + ToUnixPath(objName),
				Extension = fi.Extension,
				Size = fi.Length,
				Date = fi.CreationTime,
				MimeType = MimeType(fi.Name),
			};

			return Task.FromResult(result);
		}

		public async ValueTask<MediaStorageEntry> Preview(string path, string previewPrefix, int size)
		{
			var (storagePath, key, objName) = SplitPath(path);

			string fullPath;
			string extension = Path.GetExtension(objName).ToLower();

			if (extension == ".svg")
			{
				// no need in preview for svg-files

				fullPath = Path.Combine(storagePath, ToOSPath(objName));

				if (!File.Exists(fullPath))
					return null;

				FileInfo fi = new(fullPath);

				return new MediaStorageEntry()
				{
					Name = fi.Name,
					FullName = fi.FullName,
					RelativeName = key + "/" + ToUnixPath(objName),
					Extension = fi.Extension,
					Size = fi.Length,
					Date = fi.CreationTime,
					MimeType = MimeType(fi.Name)
				};
			}

			string cacheFolder = _settings.CacheFolder ?? DEFAULT_CACHE_FOLDER;
			string cachedName = $"{System.Web.HttpUtility.UrlEncode(previewPrefix)}_{size}x{size}.webp";
			string cachedPath = Path.Combine(cacheFolder, cachedName);

			if (File.Exists(cachedPath))
			{
				// preview already exists

				FileInfo fi = new(cachedPath);

				return new MediaStorageEntry()
				{
					Name = fi.Name,
					FullName = fi.FullName,
					RelativeName = key + "/" + ToUnixPath(objName),
					Extension = fi.Extension,
					Size = fi.Length,
					Date = fi.CreationTime,
					MimeType = MimeType(fi.Name)
				};
			}

			// generate preview and return it

			fullPath = Path.Combine(storagePath, ToOSPath(objName));

			if (!File.Exists(fullPath))
				return null;

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
			else if (_fileIconProvider != null && _fileIconProvider.TryGet(extension, size, out var bytes))
			{
				await File.WriteAllBytesAsync(cachedPath, bytes);
			}
			else if ((bytes = _fileIconProvider.Default(size)) != null)
			{
				await File.WriteAllBytesAsync(cachedPath, bytes);
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
				RelativeName = key + "/" + ToUnixPath(objName),
				Extension = f.Extension,
				Size = f.Length,
				Date = f.CreationTime,
				MimeType = MimeType(f.Name)
			};
		}

		public async Task<MediaStorageEntry> Properties(string path)
		{
			var result = await GetFile(path);

			if (result == null)
				return null;

			string extension = result.Extension.ToLower();

			if (extension == ".webp" || extension == ".png" || extension == ".jpg" ||
				extension == ".gif" || extension == ".bmp" || extension == ".tif" || extension == ".tiff")
			{
				var (storagePath, _, objName) = SplitPath(path);
				string fullPath = Path.Combine(storagePath, ToOSPath(objName));
				using var stream = File.OpenRead(fullPath);
				var image = await Image.LoadAsync(stream);

				result.Width = image.Width;
				result.Height = image.Height;
			}

			return result;
		}

		public async Task<MediaStorageEntry> Save(Stream stream, string fileName, string destination)
		{
			var (storagePath, key, objName) = SplitPath(destination);

			string relativeName = Path.Combine(ToOSPath(objName), fileName);
			string fullPath = Path.Combine(storagePath, relativeName);
			string extension = Path.GetExtension(fileName);

			byte[] buf = new byte[64 * 1024];
			long totalRead = 0;
			int read = -1;

			using (var fileStream = File.Create(fullPath))
			{
				while (read != 0)
				{
					read = await stream.ReadAsync(buf);

					if (totalRead == 0 && !CheckSignature(buf, extension))
						return null;

					totalRead += read;

					if (read > 0)
						if (totalRead <= _settings.MaxUploadSize)
							await fileStream.WriteAsync(buf.AsMemory(0, read));
						else break;
				}
			}

			if (totalRead > _settings.MaxUploadSize)
			{
				_logger.LogError("Size of '{fileName}' is greater than maximum allowed upload size.", fileName);
				_logger.LogError("Error uploading file {RelativeName}", relativeName); 
				File.Delete(fullPath);

				return null;
			}

			MediaStorageEntry result = new()
			{
				Name = fileName,
				FullName = fullPath,
				RelativeName = key + "/" + ToUnixPath(relativeName),
				Extension = extension,
				Size = totalRead,
				Date = DateTime.Now,
				MimeType = MimeType(fileName)
			};

			return result;
		}

		public async Task<string[]> Delete(string[] entries)
		{
			if (entries == null || entries.Length == 0)
				return [];

			var locations = entries
				.Select(e => {
					var (storagePath, key, path) = SplitPath(e);
					return (storagePath, key, prefix: Path.GetDirectoryName(path));
				})
				.Distinct()
				.ToArray();

			if (locations.Length > 1)
				return [];

			string storagePath = locations[0].storagePath;
			string cacheFolder = _settings.CacheFolder ?? DEFAULT_CACHE_FOLDER;

			List<EntryNames> files = [];
			List<EntryNames> folders = [];

			foreach (string entry in entries)
			{
				var (_, _, p) = SplitPath(entry);
				var name = Path.Combine(storagePath, ToOSPath(p));
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
						list.Add(folder.OsNeutralName);
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
						list.Add(file.OsNeutralName);
					}
					catch (Exception ex)
					{
						_logger?.LogWarning(ex, "Error deleting file {FullName}", file.FullName);
					}
			});

			Task task3 = Task.Run(() =>
			{
				DeletePreviews(cacheFolder, files.Select(f => f.OsNeutralName), folders.Select(f => f.OsNeutralName), _logger);
			});

			Task[] tasks = [task1, task2, task3];

			await Task.WhenAll(tasks);

			var result = list.ToArray();

			Array.Sort(result);

			return result;
		}

		public Task<MediaStorageEntry> CreateFolder(string name, string path)
		{
			if (string.IsNullOrEmpty(path))
				return Task.FromResult<MediaStorageEntry>(null);

			var (storagePath, key, objName) = SplitPath(path);

			string osPath = ToOSPath(objName);
			string fullPath = Path.Combine(storagePath, osPath, name);

			MediaStorageEntry result;

			try
			{
				var di = Directory.CreateDirectory(fullPath);

				result = new()
				{
					IsFolder = true,
					FullName = di.FullName,
					Name = di.Name,
					Extension = di.Extension,
					Date = di.CreationTime,
					RelativeName = key + "/" + ToUnixPath(Path.Combine(osPath, di.Name))
				};
			}
			catch (Exception ex)
			{
				_logger?.LogError(ex, "Error creating folder {FullPath}", fullPath);
				result = null;
			}

			return Task.FromResult(result);
		}

	}

}