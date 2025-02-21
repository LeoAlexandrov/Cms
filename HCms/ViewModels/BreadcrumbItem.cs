using System;

namespace HCms.ViewModels
{
	/// <summary>
	/// Represents a breadcrumb item view model.
	/// </summary>
	public struct BreadcrumbsItem
	{
		public int Document { get; set; }
		public string Title { get; set; }
		public string Path { get; set; }
	}
}