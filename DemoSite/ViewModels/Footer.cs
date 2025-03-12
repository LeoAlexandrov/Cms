using System;


namespace DemoSite.ViewModels
{

	public struct FooterItem
	{
		public string Label { get; set; }
		public string Link { get; set; }
	}


	public struct Footer() 
	{ 
		public FooterItem[] Links { get; set; }
	}
}