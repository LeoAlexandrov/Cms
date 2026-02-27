using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;

using AleProjects.Base64;
using HCms.Application.Dto;
using HCms.Infrastructure.Media;
using HCms.Infrastructure.Notification;


namespace HCms.Application.Services
{

	public class MediaManagementService(
		[FromKeyedServices("local")] IMediaStorage localMediaStorage,
		[FromKeyedServices("s3")] IMediaStorage s3MediaStorage,
		IAuthorizationService authService, 
		IEventNotifier notifier)
	{
		private readonly IMediaStorage _localMediaStorage = localMediaStorage;
		private readonly IMediaStorage _s3MediaStorage = s3MediaStorage;
		private readonly IAuthorizationService _authService = authService;
		private readonly IEventNotifier _notifier = notifier;

		static bool IsValidPath(string path) => string.IsNullOrEmpty(path) || !path.Contains("..");

		bool TryGetStorage(string path, out IMediaStorage storage)
		{
			if (_localMediaStorage.ServesPath(path))
			{
				storage = _localMediaStorage;
				return true;
			}

			if (_s3MediaStorage.ServesPath(path))
			{
				storage = _s3MediaStorage;
				return true;
			}

			storage = null;
			return false;
		}

		public CommonMediaStorageParams GetCommonParams(string link)
		{
			if (!Base64Url.TryDecode(link.AsSpan(), out string path))
				return CommonMediaStorageParams.Default();

			if (!TryGetStorage(path, out IMediaStorage _mediaStorage))
				return CommonMediaStorageParams.Default();

			return _mediaStorage.GetCommonParams(path);
		}

		public string GetDefaultPlace()
		{
			string[] keys = [.. _localMediaStorage.PlaceKeys, .. _s3MediaStorage.PlaceKeys];

			return keys.FirstOrDefault();
		}

		public string GetDefaultDisplayPlace()
		{
			string[] keys = [.. _localMediaStorage.PlaceKeys, .. _s3MediaStorage.PlaceKeys];

			return keys.Length == 1 ? keys[0] : null;
		}

		public async Task<Result<DtoMediaFolderReadResult>> Read(string link)
		{
			if (!Base64Url.TryDecode(link.AsSpan(), out string path))
				return Result<DtoMediaFolderReadResult>.BadParameters("Link", "Invalid base64-url format");

			if (!IsValidPath(path))
				return Result<DtoMediaFolderReadResult>.BadParameters("Link", "Invalid path");

			DtoMediaStoragePathElement[] breadcrumbs;
			List<Domain.Types.MediaStorageEntry> entries;

			if (string.IsNullOrEmpty(path))
			{
				string[] keys = [.._localMediaStorage.PlaceKeys, .._s3MediaStorage.PlaceKeys];
				entries = new(keys.Length);

				foreach (string key in keys.Order())
					entries.Add(new Domain.Types.MediaStorageEntry()
					{
						IsFolder = true,
						Name = key,
						FullName = key,
						RelativeName = key,
						Extension = string.Empty,
						Date = DateTime.UnixEpoch
					});

				breadcrumbs = [new DtoMediaStoragePathElement() { Link = string.Empty }];
			}
			else
			{
				if (!TryGetStorage(path, out IMediaStorage _mediaStorage))
					return Result<DtoMediaFolderReadResult>.BadParameters("Link", "Invalid path");

				entries = await _mediaStorage.ReadDirectory(path);

				if (entries == null)
					return Result<DtoMediaFolderReadResult>.NotFound();


				string[] labels = path.Split('/');
				int n = labels.Length;
				breadcrumbs = new DtoMediaStoragePathElement[n + 1];

				breadcrumbs[0] = new DtoMediaStoragePathElement() { Link = string.Empty };

				for (int i = 0; i < n; i++)
				{
					breadcrumbs[i + 1] = new DtoMediaStoragePathElement()
					{
						Label = labels[i],
						Link = i == n - 1 ? null : Base64Url.Encode(string.Join('/', labels, 0, i + 1))
					};
				}
			}

			return Result<DtoMediaFolderReadResult>.Success(
				new()
				{
					Entries = entries?.Select(e => new DtoMediaStorageEntry(e)),
					Path = breadcrumbs
				});
		}

		public async Task<Result<DtoPhysicalMediaFileResult>> Get(string link)
		{
			if (!Base64Url.TryDecode(link.AsSpan(), out string path))
				return Result<DtoPhysicalMediaFileResult>.BadParameters("Link", "Invalid base64-url format");

			if (!IsValidPath(path))
				return Result<DtoPhysicalMediaFileResult>.BadParameters("Link", "Invalid path");

			if (!TryGetStorage(path, out IMediaStorage _mediaStorage))
				return Result<DtoPhysicalMediaFileResult>.BadParameters("Link", "Invalid path");

			var entry = await _mediaStorage.GetFile(path);

			if (entry == null)
				return Result<DtoPhysicalMediaFileResult>.NotFound();

			return Result<DtoPhysicalMediaFileResult>.Success(new(entry));
		}

		public async Task<Result<DtoPhysicalMediaFileResult>> Preview(string link, int? size)
		{
			if (!size.HasValue)
				size = 128;
			else if (size <= 0)
				return Result<DtoPhysicalMediaFileResult>.BadParameters("Size", "Must be positive");

			if (!Base64Url.TryDecode(link.AsSpan(), out string path))
				return Result<DtoPhysicalMediaFileResult>.BadParameters("Link", "Invalid base64-url format");

			if (!IsValidPath(path))
				return Result<DtoPhysicalMediaFileResult>.BadParameters("Link", "Invalid path");

			if (!TryGetStorage(path, out IMediaStorage _mediaStorage))
				return Result<DtoPhysicalMediaFileResult>.BadParameters("Link", "Invalid path");

			var entry = await _mediaStorage.Preview(path, link, size.Value);

			if (entry == null)
				return Result<DtoPhysicalMediaFileResult>.NotFound();

			return Result<DtoPhysicalMediaFileResult>.Success(new(entry));
		}

		public async Task<Result<DtoMediaStorageEntry>> Properties(string link)
		{
			if (!Base64Url.TryDecode(link.AsSpan(), out string path))
				return Result<DtoMediaStorageEntry>.BadParameters("Link", "Invalid base64-url format");

			if (!IsValidPath(path))
				return Result<DtoMediaStorageEntry>.BadParameters("Link", "Invalid path");

			if (!TryGetStorage(path, out IMediaStorage _mediaStorage))
				return Result<DtoMediaStorageEntry>.BadParameters("Link", "Invalid path");

			var entry = await _mediaStorage.Properties(path);

			if (entry == null)
				return Result<DtoMediaStorageEntry>.NotFound();

			return Result<DtoMediaStorageEntry>.Success(new(entry));
		}

		public async Task<Result<DtoMediaStorageEntry>> Save(Stream stream, string fileName, string destinationLink, ClaimsPrincipal user)
		{
			if (!Base64Url.TryDecode(destinationLink.AsSpan(), out string destination))
				return Result<DtoMediaStorageEntry>.BadParameters("Destination", "Invalid base64-url format");

			if (string.IsNullOrEmpty(destination))
				return Result<DtoMediaStorageEntry>.BadParameters("Destination", "Media library root is read-only");

			if (!IsValidPath(destination))
				return Result<DtoMediaStorageEntry>.BadParameters("Destination", "Invalid destination");

			if (!TryGetStorage(destination, out IMediaStorage _mediaStorage))
				return Result<DtoMediaStorageEntry>.BadParameters("Destination", "Invalid destination");

			var authResult = await _authService.AuthorizeAsync(user, "UploadUnsafeContent");

			if (!authResult.Succeeded)
			{
				var provider = new FileExtensionContentTypeProvider();
				
				if (!provider.TryGetContentType(fileName, out string contentType) ||
					!contentType.StartsWith("image/") && !contentType.StartsWith("video/"))
					return Result<DtoMediaStorageEntry>.BadParameters(fileName, "Invalid file type");

				string safeNameRegex = _mediaStorage.GetCommonParams(destination).SafeNameRegex;

				if (!string.IsNullOrEmpty(safeNameRegex) && !Regex.IsMatch(fileName, safeNameRegex))
					return Result<DtoMediaStorageEntry>.BadParameters(fileName, "Unsafe file name");
			}

			var entry = await _mediaStorage.Save(stream, fileName, destination);

			if (entry == null)
				return Result<DtoMediaStorageEntry>.BadParameters(fileName, "Failed to save a file");

			await _notifier.Notify("on_media_create", [entry.RelativeName]);

			return Result<DtoMediaStorageEntry>.Success(new(entry));
		}

		public async Task<Result<string[]>> Delete(string[] links)
		{
			if (links == null || links.Length == 0)
				return Result<string[]>.Success([]);

			string[] entries = new string[links.Length];

			for (int i = 0; i < links.Length; i++)
				if (Base64Url.TryDecode(links[i].AsSpan(), out string path))
					if (IsValidPath(path))
						entries[i] = path;
					else
						return Result<string[]>.BadParameters(links[i], "Invalid path");
				else
					return Result<string[]>.BadParameters(links[i], "Invalid base64 format");

			if (entries.Any(e => string.IsNullOrEmpty(Path.GetDirectoryName(e))))
				return Result<string[]>.BadParameters("Links", "Media library root is read-only");

			if (entries.Select(e => Path.GetDirectoryName(e)).Distinct().Count() > 1)
				return Result<string[]>.BadParameters("Links", "All entries must be in the same location");

			if (!TryGetStorage(entries.First(), out IMediaStorage _mediaStorage))
				return Result<string[]>.BadParameters("Links", "Invalid path");

			var list = await _mediaStorage.Delete(entries);
			var deleted = list.Select(Base64Url.Encode).ToArray();

			await _notifier.Notify("on_media_delete", list);

			return Result<string[]>.Success(deleted);
		}

		public async Task<Result<DtoMediaStorageEntry>> CreateFolder(string name, string destination, ClaimsPrincipal user)
		{
			if (!Base64Url.TryDecode(destination.AsSpan(), out string path))
				return Result<DtoMediaStorageEntry>.BadParameters("Destination", "Invalid base64-url format");

			if (string.IsNullOrEmpty(path))
				return Result<DtoMediaStorageEntry>.BadParameters("Destination", "Media library root is read-only");

			if (!IsValidPath(path))
				return Result<DtoMediaStorageEntry>.BadParameters("Destination", "Invalid destination");

			if (!TryGetStorage(path, out IMediaStorage _mediaStorage))
				return Result<DtoMediaStorageEntry>.BadParameters("Destination", "Invalid destination");


			var authResult = await _authService.AuthorizeAsync(user, "UploadUnsafeContent");

			if (!authResult.Succeeded)
			{
				if (!Regex.IsMatch(name, "^[\\w-]+$"))
					return Result<DtoMediaStorageEntry>.BadParameters("Name", "Unsafe folder name");
			}
			else if (name.Any(c => c == '/' || c == '\\'))
			{
				return Result<DtoMediaStorageEntry>.BadParameters("Name", "Unsafe folder name");
			}

			var entry = await _mediaStorage.CreateFolder(name, path);

			if (entry == null)
				return Result<DtoMediaStorageEntry>.Other("Failed to create folder");

			return Result<DtoMediaStorageEntry>.Success(new(entry));
		}
	}
}