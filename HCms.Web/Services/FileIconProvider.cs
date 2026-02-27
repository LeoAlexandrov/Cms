using System;
using System.Collections.Generic;
using System.IO;

using HCms.Infrastructure.Media;


namespace HCms.Web.Services
{ 

	public class FileIconProvider: IFileIconProvider
	{
		const string DEFAULT_ICON_NAME = "__default.webp";

		readonly Dictionary<string, byte[]> icons = [];
		byte[] defaultIcon;

		FileIconProvider LoadFromAssets(string assetsPath)
		{
			defaultIcon = File.ReadAllBytes(Path.Combine(assetsPath, DEFAULT_ICON_NAME));
			icons.Clear();

			var files = Directory.GetFiles(assetsPath, "*.webp");

			foreach (string file in files)
				if (!string.Equals(file, DEFAULT_ICON_NAME, StringComparison.OrdinalIgnoreCase))
				{
					string iconName = Path.GetFileNameWithoutExtension(file);
					byte[] content = File.ReadAllBytes(file);
					icons[$".{iconName}"] = content;
				}

			return this;
		}

		/// <summary>
		/// Attempts to retrieve the icon data for the specified file type.
		/// </summary>
		/// <param name="filetype">The file type with leading dot for which to retrieve the icon data.</param>
		/// <param name="size">The size of the icon in pixels. Currently, only 128x128 icons are supported and this parameter is ignored.</param>
		/// <param name="bytes">When this method returns, contains the icon data as a byte array, if the operation succeeds; otherwise, <see
		/// langword="null"/>.</param>
		/// <returns><see langword="true"/> if the icon data for the specified file type was successfully retrieved; otherwise, <see
		/// langword="false"/>.</returns>
		public bool TryGet(string filetype, int size, out byte[] bytes)
		{
			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
				filetype = filetype.ToLowerInvariant();

			return icons.TryGetValue(filetype, out bytes);
		}

		/// <summary>
		/// Returns the default icon as a byte array.
		/// </summary>
		/// <param name="size">The size of the icon in pixels. Currently, only 128x128 icons are supported and this parameter is ignored.</param>
		/// <returns>A byte array representing the default icon.</returns>
		public byte[] Default(int size)
		{
			return defaultIcon;
		}

		public static FileIconProvider Load(string assetsPath)
		{
			return new FileIconProvider().LoadFromAssets(assetsPath);
		}

	}
}