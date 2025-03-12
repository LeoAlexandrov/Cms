using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using AleProjects.Cms.Domain.ValueObjects;


namespace AleProjects.Cms.Infrastructure.Media
{

	public interface IMediaStorage
	{
		List<MediaStorageEntry> ReadDirectory(string path);
		MediaStorageEntry GetFile(string path);
		ValueTask<MediaStorageEntry> Preview(string path, string previewPrefix, int size);
		Task<MediaStorageEntry> Properties(string path);
		Task<MediaStorageEntry> Save(Stream stream, string fileName, string destination);
		Task<string[]> Delete(string[] entries);
		MediaStorageEntry CreateFolder(string name, string path);
	}

}
