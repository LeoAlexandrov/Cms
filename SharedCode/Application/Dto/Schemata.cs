using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using AleProjects.Cms.Domain.Entities;
using MessagePack;


namespace AleProjects.Cms.Application.Dto
{

	[MessagePackObject]
	public class DtoSchemaResult
	{
		[MessagePack.Key("id")]
		public int Id { get; set; }

		[MessagePack.Key("namespace")]
		public string Namespace { get; set; }

		[MessagePack.Key("description")]
		public string Description { get; set; }

		[MessagePack.Key("data")] 
		public string Data { get; set; }

		[MessagePack.Key("modifiedAt")]
		public DateTimeOffset ModifiedAt { get; set; }

		public DtoSchemaResult() { }

		public DtoSchemaResult(Schema schema)
		{
			if (schema != null)
			{
				Id = schema.Id;
				Namespace = schema.Namespace;
				Description = schema.Description;
				Data = schema.Data;
				ModifiedAt = schema.ModifiedAt;
			}
		}
	}



	[MessagePackObject]
	public class DtoCreateSchema
	{
		[MessagePack.Key("description")]
		[Required(AllowEmptyStrings = false)]
		public string Description { get; set; }
	}



	[MessagePackObject]
	public class DtoUpdateSchema
	{
		[MessagePack.Key("description")]
		[Required(AllowEmptyStrings = false)]
		public string Description { get; set; }

		[MessagePack.Key("data")]
		[Required(AllowEmptyStrings = false)]
		public string Data { get; set; }
	}




}