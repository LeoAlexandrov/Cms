using System;
using System.Linq;


namespace HCms.Infrastructure.Media
{

	public class CommonMediaStorageParams
	{
		public const long DEFAULT_MAXUPLOAD_SIZE = 10 * 1024 * 1024;
		const string DEFAULT_SAFENAME_REGEX = "^[\\w-]+.\\w+$";
		
		public long? MaxUploadSize { get; set; }
		public string SafeNameRegex { get; set; }

		public static CommonMediaStorageParams Default() => new() { MaxUploadSize = DEFAULT_MAXUPLOAD_SIZE, SafeNameRegex = DEFAULT_SAFENAME_REGEX };
	}



	public class BaseMediaStorageSettings : CommonMediaStorageParams
	{
		public abstract class StoragePlace : CommonMediaStorageParams
		{
			public string Key { get; set; }
		}

		public string CacheFolder { get; set; }
	}



	public class LocalMediaStorageSettings : BaseMediaStorageSettings
	{
		public class LocalDiskPlace : StoragePlace
		{
			public string Path { get; set; } = string.Empty;
		}

		private LocalDiskPlace[] _localDiskPlaces;

		public LocalDiskPlace[] LocalDiskPlaces { get => _localDiskPlaces ?? []; set => _localDiskPlaces = value; }

		public long? OverallMaxUploadSize {
			get
			{ 
				long m = _localDiskPlaces == null || _localDiskPlaces.Length == 0 ? 0 : _localDiskPlaces.Max(p => p.MaxUploadSize ?? 0);

				if (MaxUploadSize.HasValue && MaxUploadSize.Value >= m)
					return MaxUploadSize.Value;

				return m != 0 ? m : null;
			} 
		}

		public CommonMediaStorageParams CommonParams(string placeKey)
		{
			var place = LocalDiskPlaces.FirstOrDefault(p => p.Key == placeKey);

			var result = new CommonMediaStorageParams()
			{
				MaxUploadSize = place?.MaxUploadSize ?? MaxUploadSize ?? DEFAULT_MAXUPLOAD_SIZE,
				SafeNameRegex = place?.SafeNameRegex ?? SafeNameRegex
			};

			return result;
		}
	}



	public class S3MediaStorageSettings : BaseMediaStorageSettings
	{
		public class Bucket : StoragePlace
		{
			public string Endpoint { get; set; }
			public string Name { get; set; }
			public string AccessKey { get; set; }
			public string SecretKey { get; set; }
		}

		private Bucket[] _buckets;

		public Bucket[] Buckets { get => _buckets ?? []; set => _buckets = value; }

		public long? OverallMaxUploadSize
		{
			get
			{
				long m = _buckets == null || _buckets.Length == 0 ? 0 : _buckets.Max(p => p.MaxUploadSize ?? 0);

				if (MaxUploadSize.HasValue && MaxUploadSize.Value >= m)
					return MaxUploadSize.Value;

				return m != 0 ? m : null;
			}
		}

		public CommonMediaStorageParams CommonParams(string bucketKey)
		{
			var bucket = Buckets.FirstOrDefault(b => b.Key == bucketKey);

			var result = new CommonMediaStorageParams()
			{
				MaxUploadSize = bucket?.MaxUploadSize ?? MaxUploadSize ?? DEFAULT_MAXUPLOAD_SIZE,
				SafeNameRegex = bucket?.SafeNameRegex ?? SafeNameRegex
			};

			return result;
		}
	}


}