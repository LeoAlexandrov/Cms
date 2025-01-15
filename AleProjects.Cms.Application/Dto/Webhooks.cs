using System;
using System.ComponentModel.DataAnnotations;

using MessagePack;

using AleProjects.Cms.Domain.Entities;


namespace AleProjects.Cms.Application.Dto
{

	[MessagePackObject]
	public class DtoWebhookLiteResult
	{
		[MessagePack.Key("id")]
		public int Id { get; set; }

		[MessagePack.Key("endpoint")]
		public string Endpoint { get; set; }

		[MessagePack.Key("rootDocument")]
		public int RootDocument { get; set; }

		[MessagePack.Key("enabled")]
		public bool Enabled { get; set; }


		public DtoWebhookLiteResult() {}

		internal DtoWebhookLiteResult(Webhook webhook)
		{
			if (webhook != null)
			{
				Id = webhook.Id;
				Endpoint = webhook.Endpoint;
				RootDocument = webhook.RootDocument;
				Enabled = webhook.Enabled;
			}
		}
	}



	[MessagePackObject]
	public class DtoWebhookResult : DtoWebhookLiteResult
	{
		[MessagePack.Key("secret")]
		public string Secret { get; set; }

		public DtoWebhookResult() { }

		internal DtoWebhookResult(Webhook webhook, bool includeSecret) : base(webhook)
		{
			if (webhook != null)
			{
				Secret = includeSecret ? webhook.Secret : null;
			}
		}
	}



	[MessagePackObject]
	public class DtoCreateWebhook
	{
		[MessagePack.Key("endpoint")]
		[Required(AllowEmptyStrings = false), MaxLength(260)]
		public string Endpoint { get; set; }

		[MessagePack.Key("rootDocument")]
		[RequiredPositive]
		public int RootDocument { get; set; }
	}



	[MessagePackObject]
	public class DtoUpdateWebhook
	{
		[MessagePack.Key("endpoint")]
		[Required(AllowEmptyStrings = false), MaxLength(260)]
		public string Endpoint { get; set; }

		[MessagePack.Key("rootDocument")]
		[RequiredPositive]
		public int RootDocument { get; set; }

		[MessagePack.Key("resetSecret")]
		[Required]
		public bool? ResetSecret { get; set; }

		[MessagePack.Key("enabled")]
		[Required]
		public bool? Enabled { get; set; }
	}


}