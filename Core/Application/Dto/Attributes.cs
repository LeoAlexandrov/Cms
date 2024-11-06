using System;
using System.ComponentModel.DataAnnotations;

using AleProjects.Cms.Domain.Entities;
using MessagePack;


namespace AleProjects.Cms.Application.Dto
{

	[MessagePackObject]
	public abstract class DtoAttributeResult
	{
		[MessagePack.Key("id")]
		public int Id { get; set; }

		[MessagePack.Key("attributeKey")]
		public string AttributeKey { get; set; }

		[MessagePack.Key("value")]
		public string Value { get; set; }

		[MessagePack.Key("enabled")]
		public bool Enabled { get; set; }
	}



	[MessagePackObject]
	public class DtoDocumentAttributeResult : DtoAttributeResult
	{
		[MessagePack.Key("documentRef")]
		public int DocumentRef { get; set; }

		public DtoDocumentAttributeResult() { }

		public DtoDocumentAttributeResult(DocumentAttribute attr)
		{
			if (attr != null)
			{
				Id = attr.Id;
				DocumentRef = attr.DocumentRef;
				AttributeKey = attr.AttributeKey;
				Value = attr.Value;
				Enabled = attr.Enabled;
			}
		}
	}



	[MessagePackObject]
	public class DtoFragmentAttributeResult : DtoAttributeResult
	{
		[MessagePack.Key("fragmentRef")]
		public int FragmentRef { get; set; }

		public DtoFragmentAttributeResult() { }

		public DtoFragmentAttributeResult(FragmentAttribute attr)
		{
			if (attr != null)
			{
				Id = attr.Id;
				FragmentRef = attr.FragmentRef;
				AttributeKey = attr.AttributeKey;
				Value = attr.Value;
				Enabled = attr.Enabled;
			}
		}
	}



	[MessagePackObject]
	public abstract class DtoCreateAttribute
	{
		[MessagePack.Key("attributeKey")]
		[Required(AllowEmptyStrings = false), MaxLength(128)]
		public string AttributeKey { get; set; }

		[MessagePack.Key("value")]
		public string Value { get; set; }

		[MessagePack.Key("enabled")]
		public bool Enabled { get; set; }
	}



	[MessagePackObject]
	public class DtoCreateDocumentAttribute : DtoCreateAttribute
	{
		[MessagePack.Key("documentRef")]
		[Required]
		public int DocumentRef { get; set; }
	}



	[MessagePackObject]
	public class DtoCreateFragmentAttribute : DtoCreateAttribute
	{
		[MessagePack.Key("fragmentLinkRef")]
		[Required]
		public int FragmentLinkRef { get; set; }
	}



	[MessagePackObject]
	public abstract class DtoUpdateAttribute
	{
		[MessagePack.Key("value")]
		public string Value { get; set; }

		[MessagePack.Key("enabled")]
		public bool Enabled { get; set; }
	}



	[MessagePackObject]
	public class DtoUpdateDocumentAttribute : DtoUpdateAttribute
	{
	}



	[MessagePackObject]
	public class DtoUpdateFragmentAttribute : DtoUpdateAttribute
	{
		[MessagePack.Key("documentRef")]
		[Required]
		public int DocumentRef { get; set; }
	}

}