using System;


namespace DemoSite.ViewModels
{

	public struct NavigationMenuItem
	{
		public string Label { get; set; }
		public string Link { get; set; }
		public bool Inactive { get; set; }
		public NavigationMenuItem[] Submenu { get; set; }
	}


	public struct NavigationMenu
	{
		public string Title { get; set; }
		public NavigationMenuItem[] Commands { get; set; }
	}

}