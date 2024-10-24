﻿using System;
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

using AleProjects.Base64;
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


		public Result<DtoMediaFolderReadResult> Read(string link)
		{
			if (!Base64Url.TryDecode(link, out string path))
				return Result<DtoMediaFolderReadResult>.BadParameters("Link", "Invalid base64-url format");

			var entries = _mediaStorage.ReadDirectory(path);

			if (entries == null)
				return Result<DtoMediaFolderReadResult>.NotFound();

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
						Link = i == n - 1 ? null : Base64Url.Encode(string.Join(Path.DirectorySeparatorChar, labels, 0, i + 1))
					};

				}
			}

			var url = _configuration.GetValue<string>("Media:StorageHost");

			if (!url.EndsWith('/'))
				url += "/";

			return Result<DtoMediaFolderReadResult>.Success(
				new()
				{
					Entries = entries?.Select(e => new DtoMediaStorageEntry(e, url + e.RelativeName)),
					Path = breadcrumbs
				});
		}

		public Result<DtoPhysicalMediaFileResult> Get(string link)
		{
			if (!Base64Url.TryDecode(link, out string path))
				return Result<DtoPhysicalMediaFileResult>.BadParameters("Link", "Invalid base64-url format");

			var entry = _mediaStorage.GetFile(path);

			if (entry == null)
				return Result<DtoPhysicalMediaFileResult>.NotFound();

			return Result<DtoPhysicalMediaFileResult>.Success(new(entry));
		}

		public async Task<Result<DtoMediaStorageEntry>> Properties(string link)
		{
			if (!Base64Url.TryDecode(link, out string path))
				return Result<DtoMediaStorageEntry>.BadParameters("Link", "Invalid base64-url format");

			var entry = await _mediaStorage.Properties(path);

			if (entry == null)
				return Result<DtoMediaStorageEntry>.NotFound();

			var url = _configuration.GetValue<string>("Media:StorageHost");

			if (url.EndsWith('/'))
				url += path;
			else
				url += "/" + path;


			return Result<DtoMediaStorageEntry>.Success(new(entry, url));
		}

		public async Task<Result<DtoPhysicalMediaFileResult>> Preview(string link, int? size)
		{
			if (!size.HasValue)
				size = 128;

			if (size <= 0)
				return Result<DtoPhysicalMediaFileResult>.BadParameters("Size", "Must be positive");

			if (!Base64Url.TryDecode(link, out string path))
				return Result<DtoPhysicalMediaFileResult>.BadParameters("Link", "Invalid base64-url format");

			var entry = await _mediaStorage.Preview(path, link, size.Value);

			if (entry == null)
				return Result<DtoPhysicalMediaFileResult>.NotFound();

			return Result<DtoPhysicalMediaFileResult>.Success(new(entry));
		}

		public async Task<Result<DtoMediaStorageEntry>> Save(Stream stream, string fileName, string destinationLink, ClaimsPrincipal user)
		{
			if (!Base64Url.TryDecode(destinationLink, out string destination))
				return Result<DtoMediaStorageEntry>.BadParameters("Destination", "Invalid base64-url format");

			var authResult = await _authService.AuthorizeAsync(user, "UploadUnsafeContent");

			if (!authResult.Succeeded)
			{
				var provider = new FileExtensionContentTypeProvider();
				
				if (!provider.TryGetContentType(fileName, out string contentType) ||
					(!contentType.StartsWith("image/") && !contentType.StartsWith("video/")))
					return Result<DtoMediaStorageEntry>.BadParameters(fileName, "Invalid file type");

				if (!Regex.IsMatch(fileName, SafeNameRegexString))
					return Result<DtoMediaStorageEntry>.BadParameters(fileName, "Unsafe file name");
			}

			var entry = await _mediaStorage.Save(stream, fileName, destination);

			if (entry == null)
				return Result<DtoMediaStorageEntry>.BadParameters(fileName, "Failed to save file");

			var url = _configuration.GetValue<string>("Media:StorageHost");

			if (!url.EndsWith('/'))
				url += "/";

			return Result<DtoMediaStorageEntry>.Success(new(entry, url + entry.RelativeName));
		}

		public async Task<Result<string[]>> Delete(string[] links)
		{
			if (links == null || links.Length == 0)
				return Result<string[]>.Success([]);

			string[] entries = new string[links.Length];

			for (int i = 0; i < links.Length; i++)
				if (Base64Url.TryDecode(links[i], out string path))
					entries[i] = path;
				else
					return Result<string[]>.BadParameters(links[i], "Invalid base64 format");

			var deleted = await _mediaStorage.Delete(entries);

			for (int i = 0; i < deleted.Length; i++)
				deleted[i] = Base64Url.Encode(deleted[i]);

			return Result<string[]>.Success(deleted);
		}

		public async Task<Result<DtoMediaStorageEntry>> CreateFolder(string name, string destination, ClaimsPrincipal user)
		{
			string path;

			if (!Base64Url.TryDecode(destination, out path))
				return Result<DtoMediaStorageEntry>.BadParameters("Destination", "Invalid base64-url format");

			var authResult = await _authService.AuthorizeAsync(user, "UploadUnsafeContent");

			if (!authResult.Succeeded)
			{
				if (!Regex.IsMatch(name, "^[\\w-]+$"))
					return Result<DtoMediaStorageEntry>.BadParameters("Name", "Unsafe folder name");
			}

			var entry = _mediaStorage.CreateFolder(name, path);

			return Result<DtoMediaStorageEntry>.Success(new(entry, null));
		}
	}
}