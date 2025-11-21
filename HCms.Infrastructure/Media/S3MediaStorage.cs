using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using HCms.Domain.ValueObjects;


namespace HCms.Infrastructure.Media
{

	public class S3MediaStorageSettings : BaseMediaStorageSettings
	{
		public string Endpoint { get; set; }
		public string AccessKey { get; set; }
		public string SecretKey { get; set; }
		public string BucketName { get; set; }
		public bool TopLevelFoldersAreBuckets { get; set; }
	}



	public class S3MediaStorage(IOptions<S3MediaStorageSettings> settings, ILogger<S3MediaStorage> logger) : IMediaStorage
	{
		readonly S3MediaStorageSettings _settings = settings.Value;
		readonly ILogger<S3MediaStorage> _logger = logger;

		public BaseMediaStorageSettings Settings { get => _settings; }

		public List<MediaStorageEntry> ReadDirectory(string path)
		{
			throw new NotImplementedException();
		}
		public MediaStorageEntry GetFile(string path)
		{
			throw new NotImplementedException();
		}
		public ValueTask<MediaStorageEntry> Preview(string path, string previewPrefix, int size)
		{
			throw new NotImplementedException();
		}
		public Task<MediaStorageEntry> Properties(string path)
		{
			throw new NotImplementedException();
		}
		public Task<MediaStorageEntry> Save(Stream stream, string fileName, string destination)
		{
			throw new NotImplementedException();
		}
		public Task<string[]> Delete(string[] entries)
		{
			throw new NotImplementedException();
		}
		public MediaStorageEntry CreateFolder(string name, string path)
		{
			throw new NotImplementedException();
		}
		public bool IsValidPath(string path)
		{
			throw new NotImplementedException();
		}
	}

}