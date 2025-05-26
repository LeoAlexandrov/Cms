using System;
using System.IO;


namespace AleProjects.Cms.Domain.ValueObjects
{

	public class MediaStorageEntry : IComparable<MediaStorageEntry>
	{
		public bool IsFolder { get; set; }
		public string Name { get; set; }
		public string FullName { get; set; }
		public string RelativeName { get; set; }
		public string Extension { get; set; }
		public long? Size { get; set; }
		public DateTime Date { get; set; }
		public string MimeType { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }


		public int CompareTo(MediaStorageEntry other)
		{
			if (ReferenceEquals(this, other))
				return 0;

			if (other is null)
				return 1;

			return string.Compare(
				Path.GetFileNameWithoutExtension(Name),
				Path.GetFileNameWithoutExtension(other.Name), 
				StringComparison.OrdinalIgnoreCase);
		}
	}

}