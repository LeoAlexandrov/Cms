using System;
using MessagePack;
using HCms.Domain.Types;


namespace HCms.Application.Dto
{

	[MessagePackObject]
	public class DtoTreeNode<T>
	{
		[Key("id")]
		public T Id { get; set; }

		[Key("parent")]
		public T Parent { get; set; }

		[Key("label")]
		public string Label { get; set; }

		[Key("label2")]
		public string Label2 { get; set; }

		[Key("icon")]
		public string Icon { get; set; }

		[Key("iconColor")]
		public string IconColor { get; set; }

		[Key("expandable")]
		public bool Expandable { get; set; }

		[Key("selectable")]
		public bool Selectable { get; set; }

		[Key("data")]
		public string Data { get; set; }

		[Key("children")]
		public DtoTreeNode<T>[] Children { get; set; }

		public DtoTreeNode() { }

		public static DtoTreeNode<T> Create<U>(U doc) where U : ITreeNode<T>
		{
			return new DtoTreeNode<T>()
			{
				Id = doc.Id,
				Parent = doc.Parent,
				Label = doc.Title,
				Label2 = doc.Caption,
				Icon = doc.Icon,
				IconColor = doc.Enabled ? "blue-grey" : "blue-grey-2",
				Selectable = true,
				Data = doc.Data
			};
		}

	}

}