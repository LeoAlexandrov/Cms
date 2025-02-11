using System;

namespace HCms.ViewModels
{
	public struct BreadcrumbsItem
	{
		public int Document { get; set; }
		public string Title { get; set; }
		public string Path { get; set; }
	}
}