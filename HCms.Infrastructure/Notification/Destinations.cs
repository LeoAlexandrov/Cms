using System;


namespace HCms.Infrastructure.Notification
{

	public class WebhookDestination
	{
		public string Endpoint { get; set; }
		public string Secret { get; set; }
	}



	public class RedisDestination
	{
		public string Endpoint { get; set; }
		public string User { get; set; }
		public string Password { get; set; }
		public string Channel { get; set; }
	}



	public class RabbitMQDestination
	{
		public string HostName { get; set; }
		public string User { get; set; }
		public string Password { get; set; }
		public string Exchange { get; set; }
		public string ExchangeType { get; set; }
		public string RoutingKey { get; set; }
	}
}