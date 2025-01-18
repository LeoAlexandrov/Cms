using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

using MessagePack;

using AleProjects.Cms.Domain.ValueObjects;



namespace AleProjects.Cms.Application.Dto
{
	[MessagePackObject]
	public class DtoMediaStorageEntry
	{
		[MessagePack.Key("isFolder")]
		public bool IsFolder { get; set; }

		[MessagePack.Key("name")]
		public string Name { get; set; }

		[MessagePack.Key("link")]
		public string Link { get; set; }

		[MessagePack.Key("extension")]
		public string Extension { get; set; }

		[MessagePack.Key("size")]
		public long? Size { get; set; }

		[MessagePack.Key("date")]
		public DateTime Date { get; set; }

		[MessagePack.Key("mimeType")]
		public string MimeType { get; set; }

		[MessagePack.Key("width")]
		public int Width { get; set; }

		[MessagePack.Key("height")]
		public int Height { get; set; }

		public DtoMediaStorageEntry() { }

		public DtoMediaStorageEntry(MediaStorageEntry mse) 
		{
			if (mse != null)
			{
				IsFolder = mse.IsFolder;
				Name = mse.Name;
				Link = System.Web.HttpUtility.UrlEncode(Convert.ToBase64String(Encoding.UTF8.GetBytes(mse.RelativeName)));
				Extension = mse.Extension;
				Size = mse.Size;
				Date = mse.Date;
				MimeType = mse.MimeType;
				Width = mse.Width;
				Height = mse.Height;
			}
		}
	}



	[MessagePackObject]
	public class DtoMediaStoragePathElement
	{
		[MessagePack.Key("label")]
		public string Label { get; set; }

		[MessagePack.Key("link")]
		public string Link { get; set; }
	}



	[MessagePackObject]
	public class DtoMediaFolderReadResult
	{
		[MessagePack.Key("entries")]
		public IEnumerable<DtoMediaStorageEntry> Entries { get; set; }

		[MessagePack.Key("path")]
		public IReadOnlyList<DtoMediaStoragePathElement> Path { get; set; }
	}



	[MessagePackObject]
	public class DtoPhysicalMediaFileResult
	{
		[MessagePack.Key("fullPath")]
		public string FullPath { get; set; }

		[MessagePack.Key("mimeType")]
		public string MimeType { get; set; }

		public DtoPhysicalMediaFileResult() { }

		public DtoPhysicalMediaFileResult(MediaStorageEntry mse) 
		{
			if (mse != null)
			{
				FullPath = mse.FullName;
				MimeType = mse.MimeType;
			}
		}
	}



	[MessagePackObject]
	public class DtoMediaStorageFolderCreate
	{
		[MessagePack.Key("name")]
		[Required]
		public string Name { get; set; }

		[MessagePack.Key("destination")]
		public string Destination { get; set; } 
	}



	[MessagePackObject]
	public class DtoMediaStorageEntryDelete
	{
		[MessagePack.Key("links")]
		public string[] Links { get; set; }
	}

}