using System;
using System.Collections.Generic;
using System.Linq;


namespace HCms.ViewModels
{
	/// <summary>
	/// Represents an anchor view model.
	/// </summary>
	public struct Anchor
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public int Level { get; set; }
	}
}