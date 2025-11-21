using System;

using MessagePack;


namespace HCms.Content.ViewModels
{
	/// <summary>
	/// Represents an anchor view model. Used mainly for scrollspy navigation. For use as razor page or partial view model.
	/// </summary>
	[MessagePackObject]
	public struct Anchor
	{
		[MessagePack.Key("id")]
		public string Id { get; set; }

		[MessagePack.Key("name")]
		public string Name { get; set; }

		[MessagePack.Key("level")]
		public int Level { get; set; }
	}
}