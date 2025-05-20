using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using AleProjects.Cms.Domain.ValueObjects;


namespace AleProjects.Cms.Infrastructure.Media
{

	public class BaseMediaStorageSettings
	{
		public int StorageType { get; set; } = 0; // 0 = local
		public int MaxUploadSize { get; set; } = 10 * 1024 * 1024;
		public string SafeNameRegex { get; set; } = "^[\\w-]+.\\w+$";
	}



	public interface IMediaStorage
	{
		BaseMediaStorageSettings Settings { get; }
		List<MediaStorageEntry> ReadDirectory(string path);
		MediaStorageEntry GetFile(string path);
		ValueTask<MediaStorageEntry> Preview(string path, string previewPrefix, int size);
		Task<MediaStorageEntry> Properties(string path);
		Task<MediaStorageEntry> Save(Stream stream, string fileName, string destination);
		Task<string[]> Delete(string[] entries);
		MediaStorageEntry CreateFolder(string name, string path);
	}

}
