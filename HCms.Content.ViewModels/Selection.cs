using System;
using System.Collections.Generic;

using MessagePack;


namespace HCms.Content.ViewModels
{

	[MessagePackObject]
	public class Selection
	{
		[MessagePack.Key("list")]
		public Document[] List { get; set; }

		[MessagePack.Key("totalCount")]
		public int TotalCount { get; set; }

		[MessagePack.Key("takePosition")]
		public int TakePosition { get; set; }

		[MessagePack.Key("takeCount")]
		public int TakeCount { get; set; }
	}

}