using System;

using MessagePack;


namespace HCms.Content.ViewModels
{
	/// <summary>
	/// Represents a breadcrumb item view model. For use as razor page or partial view model.
	/// </summary>
	[MessagePackObject]
	public struct BreadcrumbItem
	{
		[MessagePack.Key("document")]
		public int Document { get; set; }

		[MessagePack.Key("title")]
		public string Title { get; set; }

		[MessagePack.Key("path")]
		public string Path { get; set; }
	}
}