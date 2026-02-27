using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using HCms.Domain.Types;


namespace HCms.Infrastructure.Media
{
	public class S3MediaStorage : BaseMediaStorage, IMediaStorage
	{
		readonly S3MediaStorageSettings _settings;
		readonly IFileIconProvider _fileIconProvider;
		readonly ILogger<S3MediaStorage> _logger;
		readonly Dictionary<string, AmazonS3Client> _clients = [];

		public S3MediaStorage(IOptions<S3MediaStorageSettings> settings, IFileIconProvider fileIconProvider, ILogger<S3MediaStorage> logger)
		{
			_settings = settings.Value;
			_fileIconProvider = fileIconProvider;
			_logger = logger;

			foreach (var bucket in _settings.Buckets)
			{
				if (string.IsNullOrEmpty(bucket.Endpoint) ||
					string.IsNullOrEmpty(bucket.Name) ||
					string.IsNullOrEmpty(bucket.Key))
				{
					throw new ArgumentException("Some of bucket parameters (Endpoint, BucketName, Key) are null or empty.");
				}

				if (bucket.Key.Contains('/'))
					throw new ArgumentException("Key cannot contain '/' character.");

				var creds = new BasicAWSCredentials(bucket.AccessKey, bucket.SecretKey);

				var config = new AmazonS3Config()
				{
					ServiceURL = bucket.Endpoint, 
					ForcePathStyle = true
				};

				var client = new AmazonS3Client(creds, config);

				_clients[bucket.Name] = client;
			}
		}

		static string FolderName(string key)
		{
			ReadOnlySpan<char> chars;

			if (key.EndsWith('/'))
				chars = key.AsSpan()[..^1];
			else
				chars = key.AsSpan();

			int i = chars.LastIndexOf('/');

			return new string(chars[(i + 1) ..]);
		}

		static string FullObjectName(string key)
		{
			if (key.EndsWith('/'))
				return key[0..^1];

			return key;
		}

		(string, string, string) SplitPath(string path)
		{
			string key;
			string s3Path;
			int i = path.IndexOf('/');

			if (i < 0)
			{
				key = path;
				s3Path = string.Empty;
			}
			else
			{
				key = path[..i];
				s3Path = path[(i + 1)..];
			}

			var bucket = _settings.Buckets.FirstOrDefault(b => b.Key == key);

			return (bucket?.Name ?? string.Empty, key, s3Path);
		}

		AmazonS3Client GetClient(string bucket)
		{
			if (!_clients.TryGetValue(bucket, out var client))
			{
				_logger.LogError("No client found for bucket {bucket}", bucket);
			}

			return client;
		}

		public string[] PlaceKeys => [.. _settings.Buckets.Select(b => b.Key)];

		public bool ServesPath(string path)
		{
			var (_, key, _) = SplitPath(path);

			return _settings.Buckets.Any(b => b.Key == key);
		}

		public CommonMediaStorageParams GetCommonParams(string path)
		{
			var (_, key, _) = SplitPath(path);
			var bucket = _settings.Buckets.FirstOrDefault(b => b.Key == key);

			var result = new CommonMediaStorageParams() 
			{ 
				MaxUploadSize = bucket?.MaxUploadSize ?? _settings.MaxUploadSize, 
				SafeNameRegex = bucket?.SafeNameRegex ?? _settings.SafeNameRegex 
			};

			return result;
		}

		public async Task<List<MediaStorageEntry>> ReadDirectory(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return [.. _settings.Buckets.Select(b => new MediaStorageEntry()
				{
					IsFolder = true,
					Name = b.Key,
					FullName = b.Key,
					RelativeName = b.Key,
					Extension = string.Empty,
					Date = DateTime.UnixEpoch,
				})];
			}

			var (bucket, key, objName) = SplitPath(path);
			var client = GetClient(bucket);

			if (client == null)
				return null;

			string prefix = string.IsNullOrEmpty(objName) ? string.Empty : objName + "/";

			var result = new List<MediaStorageEntry>();
			string continuationToken = null;

			do
			{
				var request = new ListObjectsV2Request()
				{
					BucketName = bucket,
					Prefix = prefix,
					Delimiter = "/",
					ContinuationToken = continuationToken
				};

				var response = await client.ListObjectsV2Async(request);

				// folders
				if (response.CommonPrefixes != null)
					foreach (var cp in response.CommonPrefixes)
						if (!cp.StartsWith('.'))
							result.Add(
								new MediaStorageEntry()
								{
									IsFolder = true,
									Name = FolderName(cp),
									FullName = bucket + "/" + FullObjectName(cp),
									RelativeName = key + "/" + FullObjectName(cp),
									Extension = string.Empty,
									Date = DateTime.UnixEpoch
								});

				// files
				if (response.S3Objects != null)
					foreach (var s3obj in response.S3Objects)
						if (s3obj.Key != prefix && !s3obj.Key.StartsWith('.') && !s3obj.Key.EndsWith('/'))
							result.Add(
								new MediaStorageEntry()
								{
									IsFolder = false,
									Name = Path.GetFileName(s3obj.Key),
									FullName = bucket + "/" + FullObjectName(s3obj.Key),
									RelativeName = key + "/" + FullObjectName(s3obj.Key),
									Extension = Path.GetExtension(s3obj.Key),
									Date = s3obj.LastModified ?? DateTime.UnixEpoch
								});

				continuationToken = response.IsTruncated ?? false ? response.NextContinuationToken : null;
			} 
			while (continuationToken != null);

			result.Sort();

			return result;
		}

		public async Task<MediaStorageEntry> GetFile(string path)
		{
			var (bucket, key, objName) = SplitPath(path);
			var client = GetClient(bucket);

			if (client == null)
				return null;

			async ValueTask<Stream> getContentStream(GetObjectResponse r, string fn)
			{
				return new S3StreamWrapper(r);

				// alternative way with intermediate file
				/*
				TempFileStream tfs;

				try
				{
					string tempFolder = Path.Combine(_settings.CacheFolder, Guid.NewGuid().ToString());

					Directory.CreateDirectory(tempFolder);
					await r.WriteResponseStreamToFileAsync(Path.Combine(tempFolder, fn), false, default);
					tfs = new TempFileStream(tempFolder, fn);
				}
				catch (Exception ex) 
				{
					tfs = null;
					_logger.LogError(ex, "Error saving temp file {fn}", fn);
				}
				finally
				{
					r.Dispose();
				}

				return tfs;
				*/
			}


			string fileName = Path.GetFileName(objName);

			var request = new GetObjectRequest()
			{
				BucketName = bucket,
				Key = objName
			};

			var response = await client.GetObjectAsync(request);

			long size = response.ContentLength;
			DateTime lastModified = response.LastModified ?? DateTime.UnixEpoch;

			MediaStorageEntry result = new()
			{
				Name = fileName,
				FullName = bucket + "/" + objName,
				RelativeName = key + "/" + objName,
				Extension = Path.GetExtension(fileName),
				Size = size,
				Date = lastModified,
				MimeType = MimeType(fileName),
				Content = await getContentStream(response, fileName)
			};

			return result;
		}

		public async ValueTask<MediaStorageEntry> Preview(string path, string previewPrefix, int size)
		{
			var (bucket, _, objName) = SplitPath(path);
			var client = GetClient(bucket);

			if (client == null)
				return null;

			string fileExt = Path.GetExtension(objName).ToLower();
			string cacheFolder = _settings.CacheFolder ?? DEFAULT_CACHE_FOLDER;
			string cachedExt = fileExt == ".svg" ? ".svg" : ".webp";
			string cachedName = $"{System.Web.HttpUtility.UrlEncode(previewPrefix)}_{size}x{size}{cachedExt}";
			string cachedPath = Path.Combine(cacheFolder, cachedName);

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


			if (fileExt == ".svg")
			{
				var req = new GetObjectRequest() 
				{ 
					BucketName = bucket, 
					Key = objName 
				};
				
				using var resp = await client.GetObjectAsync(req);

				await resp.WriteResponseStreamToFileAsync(cachedPath, false, default);
			}
			else if (fileExt == ".webp" || fileExt == ".png" || fileExt == ".jpg" ||
					fileExt == ".gif" || fileExt == ".bmp" || fileExt == ".tif" || fileExt == ".tiff")
			{
				var req = new GetObjectRequest() 
				{ 
					BucketName = bucket, 
					Key = objName 
				};

				using var resp = await client.GetObjectAsync(req);

				var image = await Image.LoadAsync<Rgba32>(resp.ResponseStream);

				if (image.Height > size || image.Width > size)
					image.Mutate(x => x.Resize(new ResizeOptions() { Mode = ResizeMode.Pad, Size = new(size, size), PadColor = SixLabors.ImageSharp.Color.Transparent }));
				else
					image.Mutate(x => x.Pad(size, size, SixLabors.ImageSharp.Color.Transparent));

				using var output = File.OpenWrite(cachedPath);

				await image.SaveAsWebpAsync(output);
			}
			else if (_fileIconProvider != null && _fileIconProvider.TryGet(fileExt, size, out var bytes))
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

				image.Mutate(x => x.BackgroundColor(SixLabors.ImageSharp.Color.Gainsboro));

				using var output = File.OpenWrite(cachedPath);

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
			var (bucket, key, objName) = SplitPath(path);
			var client = GetClient(bucket);

			if (client == null)
				return null;

			string extension = Path.GetExtension(objName).ToLower();
			int h;
			int w;
			long size;
			DateTime lastModified;

			if (extension == ".webp" || extension == ".png" || extension == ".jpg" ||
				extension == ".gif" || extension == ".bmp" || extension == ".tif" || extension == ".tiff")
			{
				string tempFolder = Path.Combine(_settings.CacheFolder, Guid.NewGuid().ToString());

				Directory.CreateDirectory(tempFolder);

				var objReq = new GetObjectRequest() 
				{ 
					BucketName = bucket, 
					Key = objName 
				};

				using var resp = await client.GetObjectAsync(objReq);

				var image = await Image.LoadAsync(resp.ResponseStream);

				w = image.Width;
				h = image.Height;
				size = resp.ContentLength;
				lastModified = resp.LastModified ?? DateTime.UnixEpoch;
			}
			else
			{
				var objMetaReq = new GetObjectMetadataRequest()
				{
					BucketName = bucket,
					Key = objName
				};

				var metadata = await client.GetObjectMetadataAsync(objMetaReq);

				w = 0;
				h = 0;
				size = metadata.ContentLength;
				lastModified = metadata.LastModified ?? DateTime.UnixEpoch;
			}

			string name = Path.GetFileName(objName);

			MediaStorageEntry result = new()
			{
				Name = name,
				FullName = bucket + "/" + objName,
				RelativeName = key + "/" + objName,
				Extension = extension,
				Size = size,
				Date = lastModified,
				MimeType = MimeType(name),
				Width = w,
				Height = h
			};

			return result;
		}

		public async Task<MediaStorageEntry> Save(Stream stream, string fileName, string destination)
		{
			if (string.IsNullOrEmpty(destination))
				return null;

			var (bucket, key, objName) = SplitPath(destination);
			var client = GetClient(bucket);

			string tempFolder = Path.Combine(_settings.CacheFolder, Guid.NewGuid().ToString());
			string tempFileName = Path.Combine(tempFolder, fileName);
			string relativeName = string.IsNullOrEmpty(objName) ? fileName : objName + "/" + fileName;
			string extension = Path.GetExtension(fileName);
			string mimeType = MimeType(fileName);

			byte[] buf = new byte[64 * 1024];
			byte[] sha256Hash;
			long totalRead = 0;
			int read = -1;

			Directory.CreateDirectory(tempFolder);

			using (var sha256 = SHA256.Create())
			using (var fileStream = File.Create(tempFileName))
			{
				while (read != 0)
				{
					read = await stream.ReadAsync(buf.AsMemory(0, buf.Length));

					if (totalRead == 0 && !CheckSignature(buf, extension))
						return null;

					totalRead += read;

					if (read > 0)
					{
						if (totalRead <= _settings.MaxUploadSize)
							await fileStream.WriteAsync(buf.AsMemory(0, read));
						else
							break;

						sha256.TransformBlock(buf, 0, read, null, 0);
					}
				}

				sha256.TransformFinalBlock([], 0, 0);
				sha256Hash = sha256.Hash ?? [];
			}

			string sha256Hex = BitConverter.ToString(sha256Hash).Replace("-", "").ToLowerInvariant();

			MediaStorageEntry result;

			try
			{
				using var tfs = new TempFileStream(tempFolder, fileName);

				if (totalRead > _settings.MaxUploadSize)
					throw new InvalidOperationException($"Size of '{fileName}' is greater than maximum allowed upload size.");

				var putRequest = new PutObjectRequest()
				{
					BucketName = bucket,
					Key = relativeName,
					InputStream = tfs,
					ContentType = mimeType,
					AutoCloseStream = false,
					ChecksumSHA256 = sha256Hex,
					StorageClass = S3StorageClass.Standard
				};

				await client.PutObjectAsync(putRequest);

				result = new()
				{
					FullName = bucket + "/" + relativeName,
					Name = fileName,
					Extension = extension,
					Size = totalRead,
					Date = DateTime.Now,
					RelativeName = key + "/" + relativeName,
					MimeType = mimeType
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error uploading file {RelativeName}", relativeName);
				result = null;
			}

			return result;
		}

		public async Task<string[]> Delete(string[] entries)
		{
			if (entries == null || entries.Length == 0)
				return [];

			var locations = entries
				.Select(e => {
					var (bucket, key, path) = SplitPath(e);
					return (bucket, key, prefix: Path.GetDirectoryName(path));
				})
				.Distinct()
				.ToArray();

			if (locations.Length > 1)
				return [];

			string bucket = locations[0].bucket;
			string key = locations[0].key;
			string prefix = string.IsNullOrEmpty(locations[0].prefix) ? string.Empty : locations[0].prefix + "/";

			if (Path.DirectorySeparatorChar != '/')
				prefix = prefix.Replace(Path.DirectorySeparatorChar, '/');

			var client = GetClient(bucket);

			if (client == null)
				return [];

			var toDelete = new HashSet<string>(entries.Select(e => SplitPath(e).Item3));

			static async Task<(List<string>, List<string>)> gatherAllObjects(IAmazonS3 client, string bucketName, string prefix)
			{
				var files = new List<string>();
				var folders = new List<string>();

				string continuationToken = null;

				do
				{
					var req = new ListObjectsV2Request() 
					{ 
						BucketName = bucketName, 
						Prefix = prefix, 
						ContinuationToken = continuationToken, 
						Delimiter = "/" 
					};

					var resp = await client.ListObjectsV2Async(req);

					if (resp.CommonPrefixes != null)
						foreach (var cp in resp.CommonPrefixes)
							folders.Add(cp.TrimEnd('/'));

					if (resp.S3Objects != null)
						foreach (var obj in resp.S3Objects)
							if (!obj.Key.EndsWith('/'))
								files.Add(obj.Key);

					continuationToken = resp.IsTruncated ?? false ? resp.NextContinuationToken : null;
				} 
				while (continuationToken != null);

				return (files, folders);
			}

			var (files, folders) = await gatherAllObjects(client, bucket, prefix);

			static async Task removeFiles(IAmazonS3 client, IEnumerable<string> files, ConcurrentBag<string> reportTo, string bucketName, string alias, ILogger<S3MediaStorage> logger)
			{
				var deleteReq = new DeleteObjectsRequest() { BucketName = bucketName };

				foreach (var file in files)
				{
					deleteReq.AddKey(file);
					reportTo?.Add(alias + "/" + file);
				}

				try
				{
					await client.DeleteObjectsAsync(deleteReq);
				}
				catch (Exception ex)
				{
					logger?.LogWarning(ex, "Error deleting files");
				}
			}

			static async Task removeFolders(IAmazonS3 client, IEnumerable<string> folders, ConcurrentBag<string> reportTo, string bucketName, string alias, ILogger<S3MediaStorage> logger)
			{
				var deleteReq = new DeleteObjectRequest() { BucketName = bucketName };

				foreach (var folder in folders)
				{
					string pfx = folder + "/";
					var (subFiles, subFolders) = await gatherAllObjects(client, bucketName, pfx);

					if (subFiles.Count != 0 || subFolders.Count != 0)
					{
						await removeFiles(client, subFiles, null, bucketName, alias, logger);
						await removeFolders(client, subFolders, null, bucketName, alias, logger);
					}

					deleteReq.Key = pfx;

					try
					{
						await client.DeleteObjectAsync(deleteReq);
						reportTo?.Add(alias + "/" + folder);
					}
					catch (Exception ex)
					{
						logger?.LogWarning(ex, "Error deleting folder {Folder}", folder);
					}
				}
			}

			ConcurrentBag<string> list = [];
			var topFiles = files.Where(f => toDelete.Contains(f)).ToList();
			var topFolders = folders.Where(f => toDelete.Contains(f)).ToList();
			string cacheFolder = _settings.CacheFolder ?? DEFAULT_CACHE_FOLDER;

			Task task1 = Task.Run(async () => await removeFiles(client, topFiles, list, bucket, key, _logger));
			Task task2 = Task.Run(async () => await removeFolders(client, topFolders, list, bucket, key, _logger));
			Task task3 = Task.Run(() => DeletePreviews(cacheFolder, topFiles, topFolders, _logger));

			Task[] tasks = [task1, task2, task3];

			await Task.WhenAll(tasks);

			var result = list.ToArray();

			Array.Sort(result);

			return result;
		}

		public async Task<MediaStorageEntry> CreateFolder(string name, string path)
		{
			if (string.IsNullOrEmpty(path))
				return null;

			var (bucket, key, objName) = SplitPath(path);
			var client = GetClient(bucket);

			string objectName = string.IsNullOrEmpty(objName) ? name : objName + "/" + name;
			MediaStorageEntry result;

			try
			{
				using var ms = new MemoryStream();

				var putRequest = new PutObjectRequest()
				{
					BucketName = bucket,
					Key = objectName + "/",
					InputStream = ms,
					ContentBody = string.Empty
				};

				await client.PutObjectAsync(putRequest);

				result = new()
				{
					IsFolder = true,
					FullName = bucket + "/" + objectName,
					Name = name,
					Extension = Path.GetExtension(name),
					Date = DateTime.Now,
					RelativeName = key + "/" + objectName
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating folder {ObjectName}", objectName);
				result = null;
			}

			return result;
		}
	}
}