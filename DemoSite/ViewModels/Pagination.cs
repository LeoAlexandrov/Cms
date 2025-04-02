using System;
using System.Collections.Generic;

namespace DemoSite.ViewModels
{

	public struct PaginationLink
	{
		public string Label { get; set; }
		public string Link { get; set; }
	}


	public struct Pagination() 
	{ 
		public List<PaginationLink> Links { get; set; }
	}
}