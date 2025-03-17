using System;


namespace DemoSite.ViewModels
{

	public struct MainMenuItem
	{
		public string Label { get; set; }
		public string Link { get; set; }
		public bool Active { get; set; }
	}


	public struct MainMenu() 
	{ 
		public MainMenuItem[] Languages { get; set; }
		public MainMenuItem[] Commands { get; set; }
	}
}