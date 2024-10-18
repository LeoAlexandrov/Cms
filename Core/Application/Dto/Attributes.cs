using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using AleProjects.Cms.Domain.Entities;
using MessagePack;


namespace AleProjects.Cms.Application.Dto
{

	[MessagePackObject]
	public class DtoDocumentAttributeResult
	{
		[MessagePack.Key("id")]
		public int Id { get; set; }

		[MessagePack.Key("documentRef")]
		public int DocumentRef { get; set; }

		[MessagePack.Key("attributeKey")]
		public string AttributeKey { get; set; }

		[MessagePack.Key("value")]
		public string Value { get; set; }

		[MessagePack.Key("enabled")]
		public bool Enabled { get; set; }

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
	public class DtoCreateDocumentAttribute
	{
		[MessagePack.Key("documentRef")]
		[Required]
		public int DocumentRef { get; set; }

		[MessagePack.Key("attributeKey")]
		[Required(AllowEmptyStrings = false), MaxLength(128)]
		public string AttributeKey { get; set; }

		[MessagePack.Key("value")]
		public string Value { get; set; }

		[MessagePack.Key("enabled")]
		public bool Enabled { get; set; }
	}



	[MessagePackObject]
	public class DtoUpdateDocumentAttribute
	{
		[MessagePack.Key("value")]
		public string Value { get; set; }

		[MessagePack.Key("enabled")]
		public bool Enabled { get; set; }
	}

}