using System;
using System.Collections.Generic;

using AleProjects.Cms.Application.Dto;


namespace AleProjects.Cms.Application.Services
{

	public struct ReadMediaFolderResult
	{
		public DtoMediaFolderReadResult Result { get; set; }
		public bool NotFound { get; set; }
		public bool BadParameters { get; set; }
		public object Errors { get; set; }

		public static ReadMediaFolderResult FolderNotFound() => new() { NotFound = true };
		public static ReadMediaFolderResult BadFolderParameters(object errors) => new() { BadParameters = true, Errors = new { errors } };
		public static ReadMediaFolderResult Success(IEnumerable<DtoMediaStorageEntry> entries, IReadOnlyList<DtoMediaStoragePathElement> path) => 
			new() { Result = new() { Entries = entries, Path = path } };
	}



	public struct PhysicalMediaFileResult
	{
		public DtoPhysicalMediaFileResult Result { get; set; }
		public bool NotFound { get; set; }
		public bool BadParameters { get; set; }
		public object Errors { get; set; }

		public static PhysicalMediaFileResult FileNotFound() => new() { NotFound = true };
		public static PhysicalMediaFileResult BadFileParameters(object errors) => new() { BadParameters = true, Errors = new { errors } };
		public static PhysicalMediaFileResult Success(DtoPhysicalMediaFileResult file) => new() { Result = file };
	}



	public struct MediaFilePropertiesResult
	{
		public DtoMediaStorageEntry Result { get; set; }
		public bool NotFound { get; set; }
		public bool BadParameters { get; set; }
		public object Errors { get; set; }

		public static MediaFilePropertiesResult FileNotFound() => new() { NotFound = true };
		public static MediaFilePropertiesResult BadFileParameters(object errors) => new() { BadParameters = true, Errors = new { errors } };
		public static MediaFilePropertiesResult Success(DtoMediaStorageEntry file) => new() { Result = file };
	}



	public struct UploadMediaFileResult
	{
		public DtoMediaStorageEntry Result { get; set; }
		public bool BadParameters { get; set; }
		public object Errors { get; set; }

		public static UploadMediaFileResult BadFileParameters(object errors) => new() { BadParameters = true, Errors = new { errors } };
		public static UploadMediaFileResult Success(DtoMediaStorageEntry file) => new() { Result = file };
	}



	public struct DeleteMediaEntriesResult
	{
		public IList<string> Result { get; set; }
		public bool BadParameters { get; set; }
		public object Errors { get; set; }

		public static DeleteMediaEntriesResult BadFileParameters(object errors) => new() { BadParameters = true, Errors = new { errors } };
		public static DeleteMediaEntriesResult Success(IList<string> deleted) => new() { Result = deleted };
	}



	public struct CreateMediaFolderResult
	{
		public DtoMediaStorageEntry Result { get; set; }
		public bool BadParameters { get; set; }
		public object Errors { get; set; }

		public static CreateMediaFolderResult BadFolderParameters(object errors) => new() { BadParameters = true, Errors = new { errors } };
		public static CreateMediaFolderResult Success(DtoMediaStorageEntry folder) => new() { Result = folder };
	}

}