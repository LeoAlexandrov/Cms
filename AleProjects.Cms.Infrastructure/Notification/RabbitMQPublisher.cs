using System;
using System.IO;
using System.Threading.Tasks;

using RabbitMQ.Client;

using AleProjects.Cms.Domain.ValueObjects;
using AleProjects.Endpoints;
using System.Net;


namespace AleProjects.Cms.Infrastructure.Notification
{

	public static class RabbitMQPublisher
	{
		public static async Task Publish(RabbitMQDestination destination, EventPayload payload)
		{
			string host;
			int port;

			if (EndPoints.TryParse(destination.HostName, out EndPoint endpoint))
			{
				if (endpoint is IPEndPoint ipEndPoint)
				{
					host = ipEndPoint.Address.ToString();
					port = ipEndPoint.Port;
				}
				else if (endpoint is DnsEndPoint dnsEndPoint)
				{
					host = dnsEndPoint.Host;
					port = dnsEndPoint.Port;
				}
				else
				{
					host = destination.HostName;
					port = AmqpTcpEndpoint.UseDefaultPort;
				}
			}
			else
			{
				host = destination.HostName;
				port = AmqpTcpEndpoint.UseDefaultPort;
			}


			var factory = new ConnectionFactory() { HostName = host, Port = port };

			if (!string.IsNullOrEmpty(destination.User))
			{
				factory.UserName = destination.User;

				if (!string.IsNullOrEmpty(destination.Password))
					factory.Password = destination.Password;
			}

			try
			{
				using var connection = await factory.CreateConnectionAsync();
				using var channel = await connection.CreateChannelAsync();

				await channel.ExchangeDeclareAsync(exchange: destination.Exchange, type: destination.ExchangeType);

				using MemoryStream ms = new();
				System.Text.Json.JsonSerializer.Serialize(ms, payload);

				var body = ms.ToArray();
				await channel.BasicPublishAsync(destination.Exchange, destination.RoutingKey ?? string.Empty, body);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"RabbitMQ destination '{destination.HostName}' failed with exception: {ex.Message}");
			}
		}
	}
}