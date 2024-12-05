using System;

using AleProjects.Cms.Domain.ValueObjects;
using MessagePack;


namespace AleProjects.Cms.Application.Dto
{

	[MessagePackObject]
	public class DtoTreeNode<T>
	{
		[MessagePack.Key("id")]
		public T Id { get; set; }

		[MessagePack.Key("parent")]
		public T Parent { get; set; }

		[MessagePack.Key("label")]
		public string Label { get; set; }

		[MessagePack.Key("label2")]
		public string Label2 { get; set; }

		[MessagePack.Key("icon")]
		public string Icon { get; set; }

		[MessagePack.Key("iconColor")]
		public string IconColor { get; set; }

		[MessagePack.Key("expandable")]
		public bool Expandable { get; set; }

		[MessagePack.Key("selectable")]
		public bool Selectable { get; set; }

		[MessagePack.Key("data")]
		public string Data { get; set; }

		[MessagePack.Key("children")]
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