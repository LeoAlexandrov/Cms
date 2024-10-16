using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.StaticFiles;

using AleProjects.Cms.Application.Dto;
using AleProjects.Cms.Infrastructure.Media;



namespace AleProjects.Cms.Application.Services
{

	public class MediaManagementService(MediaStorage mediaStorage, IAuthorizationService authService, IConfiguration configuration)
	{
		const int DEFAULT_MAX_UPLOAD_SIZE = 10 * 1024 * 1024;
		const string DEFAULT_SAFENAME_REGEX = ".+";

		private readonly MediaStorage _mediaStorage = mediaStorage;
		private readonly IAuthorizationService _authService = authService;
		private readonly IConfiguration _configuration = configuration;


		public int MaxUploadSize
		{
			get => _configuration.GetValue<int>("Media:MaxUploadSize", DEFAULT_MAX_UPLOAD_SIZE);
		}

		public string SafeNameRegexString
		{
			get => _configuration.GetValue<string>("Media:SafeNameRegex", DEFAULT_SAFENAME_REGEX);
		}


		public ReadMediaFolderResult Read(string link)
		{
			if (!ReferencesHelper.TryFromBase64(link, out string path))
				return ReadMediaFolderResult.BadFolderParameters(ModelErrors.Create("Link", "Invalid base64 format"));

			var entries = _mediaStorage.ReadDirectory(path);

			if (entries == null)
				return ReadMediaFolderResult.FolderNotFound();

			DtoMediaStoragePathElement[] breadcrumbs;

			if (string.IsNullOrEmpty(path))
			{
				breadcrumbs = [new DtoMediaStoragePathElement()];
			}
			else
			{
				string[] labels = path.Split(Path.DirectorySeparatorChar);
				int n = labels.Length;
				breadcrumbs = new DtoMediaStoragePathElement[n + 1];

				breadcrumbs[0] = new DtoMediaStoragePathElement() { Link = "" };

				for (int i = 0; i < n; i++)
				{
					breadcrumbs[i + 1] = new DtoMediaStoragePathElement()
					{
						Label = labels[i],
						Link = i == n - 1 ? null : System.Web.HttpUtility.UrlEncode(Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(Path.DirectorySeparatorChar, labels, 0, i + 1))))
					};

				}
			}

			var url = _configuration.GetValue<string>("Media:StorageHost");

			if (!url.EndsWith('/'))
				url += "/";

			return ReadMediaFolderResult.Success(entries?.Select(e => new DtoMediaStorageEntry(e, url + e.RelativeName)), breadcrumbs);
		}

		public PhysicalMediaFileResult Get(string link)
		{
			if (!ReferencesHelper.TryFromBase64(link, out string path))
				return PhysicalMediaFileResult.BadFileParameters(ModelErrors.Create("Link", "Invalid base64 format"));

			var entry = _mediaStorage.GetFile(path);

			if (entry == null)
				return PhysicalMediaFileResult.FileNotFound();

			return PhysicalMediaFileResult.Success(new(entry));
		}

		public async Task<MediaFilePropertiesResult> Properties(string link)
		{
			if (!ReferencesHelper.TryFromBase64(link, out string path))
				return MediaFilePropertiesResult.BadFileParameters(ModelErrors.Create("Link", "Invalid base64 format"));

			var entry = await _mediaStorage.Properties(path);

			if (entry == null)
				return MediaFilePropertiesResult.FileNotFound();

			var url = _configuration.GetValue<string>("Media:StorageHost");

			if (url.EndsWith('/'))
				url += path;
			else
				url += "/" + path;


			return MediaFilePropertiesResult.Success(new(entry, url));
		}

		public async Task<PhysicalMediaFileResult> Preview(string link, int? size)
		{
			if (!size.HasValue)
				size = 128;

			if (size <= 0)
				return PhysicalMediaFileResult.BadFileParameters(ModelErrors.Create("Size", "Must be positive"));

			if (!ReferencesHelper.TryFromBase64(link, out string path))
				return PhysicalMediaFileResult.BadFileParameters(ModelErrors.Create("Link", "Invalid base64 format"));

			var entry = await _mediaStorage.Preview(path, link, size.Value);

			if (entry == null)
				return PhysicalMediaFileResult.FileNotFound();

			return PhysicalMediaFileResult.Success(new(entry));
		}

		public async Task<UploadMediaFileResult> Save(Stream stream, string fileName, string destinationLink, ClaimsPrincipal user)
		{
			if (!ReferencesHelper.TryFromBase64(destinationLink, out string destination))
				return UploadMediaFileResult.BadFileParameters(ModelErrors.Create("Destination", "Invalid base64 format"));

			var authResult = await _authService.AuthorizeAsync(user, "UploadUnsafeContent");

			if (!authResult.Succeeded)
			{
				var provider = new FileExtensionContentTypeProvider();
				
				if (!provider.TryGetContentType(fileName, out string contentType) ||
					(!contentType.StartsWith("image/") && !contentType.StartsWith("video/")))
					return UploadMediaFileResult.BadFileParameters(ModelErrors.Create(fileName, "Invalid file type"));

				if (!Regex.IsMatch(fileName, SafeNameRegexString))
					return UploadMediaFileResult.BadFileParameters(ModelErrors.Create(fileName, "Unsafe file name"));
			}

			var entry = await _mediaStorage.Save(stream, fileName, destination);

			if (entry == null)
				return UploadMediaFileResult.BadFileParameters(ModelErrors.Create(fileName, "Failed to save file"));

			var url = _configuration.GetValue<string>("Media:StorageHost");

			if (!url.EndsWith('/'))
				url += "/";

			return UploadMediaFileResult.Success(new(entry, url + entry.RelativeName));
		}

		public async Task<DeleteMediaEntriesResult> Delete(string[] links)
		{
			if (links == null || links.Length == 0)
				return DeleteMediaEntriesResult.Success([]);

			string[] entries = new string[links.Length];

			for (int i = 0; i < links.Length; i++)
				if (ReferencesHelper.TryFromBase64(System.Web.HttpUtility.UrlDecode(links[i]), out string path))
					entries[i] = path;
				else
					return DeleteMediaEntriesResult.BadFileParameters(ModelErrors.Create(links[i], "Invalid base64 format"));

			var deleted = await _mediaStorage.Delete(entries);

			for (int i = 0; i < deleted.Count; i++)
				deleted[i] = System.Web.HttpUtility.UrlEncode(Convert.ToBase64String(Encoding.UTF8.GetBytes(deleted[i])));

			return DeleteMediaEntriesResult.Success(deleted);
		}

		public async Task<CreateMediaFolderResult> CreateFolder(string name, string destination, ClaimsPrincipal user)
		{
			string path;

			if (!ReferencesHelper.TryFromBase64(System.Web.HttpUtility.UrlDecode(destination), out path))
				return CreateMediaFolderResult.BadFolderParameters(ModelErrors.Create("Destination", "Invalid base64 format"));

			var authResult = await _authService.AuthorizeAsync(user, "UploadUnsafeContent");

			if (!authResult.Succeeded)
			{
				if (!Regex.IsMatch(name, "^[\\w-]+$"))
					return CreateMediaFolderResult.BadFolderParameters(ModelErrors.Create("Name", "Unsafe folder name"));
			}

			var entry = _mediaStorage.CreateFolder(name, path);

			return CreateMediaFolderResult.Success(new(entry, null));
		}
	}
}