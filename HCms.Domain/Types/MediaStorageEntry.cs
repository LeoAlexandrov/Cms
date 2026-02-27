using System;
using System.IO;


namespace HCms.Domain.Types
{
	/// <summary>
	/// Represents an entry in the media library storage, which can be either a file or a folder.
	/// </summary>
	public class MediaStorageEntry : IComparable<MediaStorageEntry>
	{
		/// <summary>
		/// Value indicating whether the item represents a folder.
		/// </summary>
		public bool IsFolder { get; set; }
		
		/// <summary>
		/// Gets or sets the file name of the object without the path.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Full name of the object including path and name.
		/// </summary>
		/// <remarks>For file media storage also includes the root of the media storage. For S3 storage it includes bucket name alias.
		/// </remarks>
		public string FullName { get; set; }

		/// <summary>
		/// Name of the object and path relative to the root of file media storage. For S3 storage it is equal to FullName property.
		/// </summary>
		/// <remarks>This property must always use Linux directory separator char '/' unlike the FullName property
		/// using system specific separator char.</remarks>
		public string RelativeName { get; set; }
		
		/// <summary>
		/// Gets or sets the file extension, including the leading period (e.g., ".txt").
		/// </summary>
		public string Extension { get; set; }

		/// <summary>
		/// Gets or sets the size of the file in bytes. Null for folders.
		/// </summary>
		public long? Size { get; set; }

		/// <summary>
		/// Gets or sets the date and time when the file or folder was last modified.
		/// </summary>
		public DateTime Date { get; set; }
		
		/// <summary>
		/// Gets or sets the MIME type associated with the content.
		/// </summary>
		public string MimeType { get; set; }
		
		/// <summary>
		/// Gets or sets the width if the object is an image.
		/// </summary>
		public int Width { get; set; }

		/// <summary>
		/// Gets or sets the height if the object is an image.
		/// </summary>
		public int Height { get; set; }

		public Stream Content { get; set; }


		public int CompareTo(MediaStorageEntry other)
		{
			if (ReferenceEquals(this, other))
				return 0;

			if (other is null)
				return 1;

			if (IsFolder ^ other.IsFolder)
				return IsFolder ? -1 : 1;

			return string.Compare(
				Path.GetFileNameWithoutExtension(Name),
				Path.GetFileNameWithoutExtension(other.Name), 
				StringComparison.OrdinalIgnoreCase);
		}
	}

}