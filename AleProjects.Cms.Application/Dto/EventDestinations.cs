using System;
using System.ComponentModel.DataAnnotations;

using MessagePack;

using AleProjects.Cms.Domain.Entities;


namespace AleProjects.Cms.Application.Dto
{
	[MessagePackObject]
	public class DtoEventDestinationLiteResult
	{
		[MessagePack.Key("id")]
		public int Id { get; set; }

		[MessagePack.Key("type")]
		public string Type { get; set; }

		[MessagePack.Key("name")] 
		public string Name { get; set; }

		[MessagePack.Key("triggeringPath")]
		public string TriggeringPath { get; set; }

		[MessagePack.Key("triggeringPathAux")]
		public string TriggeringPathAux { get; set; }

		[MessagePack.Key("enabled")]
		public bool Enabled { get; set; }

		public DtoEventDestinationLiteResult() { }

		internal DtoEventDestinationLiteResult(EventDestination destination)
		{
			if (destination != null)
			{
				Id = destination.Id;
				Type = destination.Type;
				Name = destination.Name;
				TriggeringPath = destination.TriggeringPath;
				TriggeringPathAux = destination.TriggeringPathAux;
				Enabled = destination.Enabled;
			}
		}
	}



	[MessagePackObject]
	public class DtoWebhookDestinationResult
	{
		[MessagePack.Key("endpoint")]
		public string Endpoint { get; set; }

		[MessagePack.Key("secret")]
		public string Secret { get; set; }
	}



	[MessagePackObject]
	public class DtoWebhookDestination
	{
		[MessagePack.Key("endpoint")]
		[Required(AllowEmptyStrings = false), MaxLength(260)]
		public string Endpoint { get; set; }

		[MessagePack.Key("resetSecret")]
		public bool ResetSecret { get; set; }
	}



	[MessagePackObject]
	public class DtoRedisDestination
	{
		[MessagePack.Key("endpoint")]
		[Required(AllowEmptyStrings = false)]
		public string Endpoint { get; set; }

		[MessagePack.Key("user")]
		public string User { get; set; }

		[MessagePack.Key("password")]
		public string Password { get; set; }

		[MessagePack.Key("channel")]
		[Required(AllowEmptyStrings = false), MaxLength(260)]
		public string Channel { get; set; }
	}



	[MessagePackObject]
	public class DtoRedisDestinationResult : DtoRedisDestination
	{
	}



	[MessagePackObject]
	public class DtoRabbitMQDestination
	{
		[MessagePack.Key("hostName")]
		[Required(AllowEmptyStrings = false), MaxLength(260)]
		public string HostName { get; set; }

		[MessagePack.Key("user")]
		public string User { get; set; }

		[MessagePack.Key("password")]
		public string Password { get; set; }

		[MessagePack.Key("exchange")]
		[Required(AllowEmptyStrings = false)]
		public string Exchange { get; set; }

		[MessagePack.Key("exchangeType")]
		[Required(AllowEmptyStrings = false)]
		public string ExchangeType { get; set; }

		[MessagePack.Key("routingKey")]
		public string RoutingKey { get; set; }
	}



	[MessagePackObject]
	public class DtoRabbitMQDestinationResult : DtoRabbitMQDestination
	{
	}



	[MessagePackObject]
	public class DtoEventDestinationResult : DtoEventDestinationLiteResult
	{
		[MessagePack.Key("webhook")]
		public DtoWebhookDestinationResult Webhook { get; set; }

		[MessagePack.Key("redis")]
		public DtoRedisDestinationResult Redis { get; set; }

		[MessagePack.Key("rabbitMq")]
		public DtoRabbitMQDestinationResult RabbitMQ { get; set; }

		public DtoEventDestinationResult() { }

		public DtoEventDestinationResult(EventDestination destination, bool includeSensitive) : base(destination)
		{
			if (destination != null)
			{
				switch (destination.Type)
				{
					case "webhook":
						Webhook = System.Text.Json.JsonSerializer.Deserialize<DtoWebhookDestinationResult>(destination.Data);

						if (!includeSensitive)
							Webhook.Secret = null;

						break;

					case "redis":
						Redis = System.Text.Json.JsonSerializer.Deserialize<DtoRedisDestinationResult>(destination.Data);

						if (!includeSensitive)
							Redis.Password = null;

						break;

					case "rabbitmq":
						
						RabbitMQ = System.Text.Json.JsonSerializer.Deserialize<DtoRabbitMQDestinationResult>(destination.Data);
						
						if (!includeSensitive)
							RabbitMQ.Password = null;
						
						break;

				}
			}
		}

	}



	[MessagePackObject]
	public class DtoCreateEventDestination
	{
		[MessagePack.Key("type")]
		[Required(AllowEmptyStrings = false), MaxLength(16)]
		public string Type { get; set; }

		[MessagePack.Key("name")]
		[Required(AllowEmptyStrings = false), MaxLength(128)]
		public string Name { get; set; }

	}



	[MessagePackObject]
	public class DtoUpdateEventDestination
	{
		[MessagePack.Key("name")]
		[Required(AllowEmptyStrings = false), MaxLength(128)]
		public string Name { get; set; }

		[MessagePack.Key("enabled")]
		[Required]
		public bool? Enabled { get; set; }

		[MessagePack.Key("triggeringPath")]
		public string TriggeringPath { get; set; }

		[MessagePack.Key("triggeringPathAux")]
		public string TriggeringPathAux { get; set; }


		[MessagePack.Key("webhook")]
		public DtoWebhookDestination Webhook { get; set; }

		[MessagePack.Key("redis")]
		public DtoRedisDestination Redis { get; set; }

		[MessagePack.Key("rabbitMq")]
		public DtoRabbitMQDestination RabbitMQ { get; set; }

	}


}