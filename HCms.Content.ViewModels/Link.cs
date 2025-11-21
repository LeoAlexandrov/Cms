using System;
using System.Collections.Generic;


namespace HCms.Content.ViewModels
{
	public interface ILink
	{
		string Label { get; set; }
		string Link { get; set; }
	}
}