using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using StackExchange.Redis;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using AleProjects.Endpoints;
using HCms.Dto;
using System.Net;


namespace DemoSite.Services
{

	public class EventSubscriptionService(IConfiguration configuration, IServiceScopeFactory serviceScopeFactory) : IDisposable, IHostedService
	{
		readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

		readonly string redisEndpoints = configuration["Redis:Endpoints"];
		readonly string redisUser = configuration["Redis:User"];
		readonly string redisPassword = configuration["Redis:Password"];
		readonly RedisChannel redisChannel = RedisChannel.Literal(configuration["Redis:Channel"]);
		
		readonly string rabbitHost = configuration["Rabbit:Host"];
		readonly string rabbitUser = configuration["Rabbit:User"];
		readonly string rabbitPassword = configuration["Rabbit:Password"];
		readonly string rabbitExchange = configuration["Rabbit:Exchange"];
		readonly string rabbitExchangeType = configuration["Rabbit:ExchangeType"];
		readonly string rabbitRoutingKey = configuration["Rabbit:RoutingKey"];

		ConnectionMultiplexer redisConnection;
		ISubscriber redisSubscriber;

		IConnection rabbitConnection;
		IChannel rabbitChannel;
		AsyncEventingBasicConsumer rabbitConsumer;


		void SubscribeRedis()
		{
			ConfigurationOptions opts = new()
			{
				EndPoints = [.. EndPoints.Parse(redisEndpoints)],
			};

			if (!string.IsNullOrEmpty(redisUser))
			{
				opts.User = redisUser;

				if (!string.IsNullOrEmpty(redisPassword))
					opts.Password = redisPassword;
			}

			redisConnection = ConnectionMultiplexer.Connect(opts);
			redisSubscriber = redisConnection.GetSubscriber();

			redisSubscriber.Subscribe(redisChannel, RedisEventHandler);
		}

		async Task SubscribeRabbit()
		{
			string host;
			int port;

			if (EndPoints.TryParse(rabbitHost, out EndPoint endpoint))
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
					host = rabbitHost;
					port = AmqpTcpEndpoint.UseDefaultPort;
				}
			}
			else
			{
				host = rabbitHost;
				port = AmqpTcpEndpoint.UseDefaultPort;
			}


			var factory = new ConnectionFactory { HostName = host, Port = port };

			if (!string.IsNullOrEmpty(rabbitUser))
			{
				factory.UserName = rabbitUser;

				if (!string.IsNullOrEmpty(rabbitPassword))
					factory.Password = rabbitPassword;
			}

			rabbitConnection = await factory.CreateConnectionAsync();
			rabbitChannel = await rabbitConnection.CreateChannelAsync();

			await rabbitChannel.ExchangeDeclareAsync(exchange: rabbitExchange, type: rabbitExchangeType ?? ExchangeType.Fanout);

			QueueDeclareOk queueDeclareResult = await rabbitChannel.QueueDeclareAsync();
			string queueName = queueDeclareResult.QueueName;

			await rabbitChannel.QueueBindAsync(queue: queueName, exchange: rabbitExchange, routingKey: rabbitRoutingKey ?? string.Empty);

			rabbitConsumer = new AsyncEventingBasicConsumer(rabbitChannel);

			rabbitConsumer.ReceivedAsync += RabbitEventHandler;

			await rabbitChannel.BasicConsumeAsync(queueName, autoAck: true, consumer: rabbitConsumer);
		}


		public async Task StartAsync(CancellationToken cancellationToken)
		{
			if (!string.IsNullOrEmpty(redisEndpoints))
				SubscribeRedis();
			
			if (!string.IsNullOrEmpty(rabbitHost))
				await SubscribeRabbit();

			return;
		}

		public void RedisEventHandler(RedisChannel channel, RedisValue message)
		{
			using (var scope = _serviceScopeFactory.CreateScope())
			{
				var cmsService = scope.ServiceProvider.GetRequiredService<CmsContentService>();
				var payload = System.Text.Json.JsonSerializer.Deserialize<EventPayload>(message);

				cmsService.UpdateCache(payload);
			}

			Console.WriteLine("Message received from redis pubsub");
		}

		public Task RabbitEventHandler(object sender, BasicDeliverEventArgs ea)
		{
			using (var scope = _serviceScopeFactory.CreateScope())
			{
				var cmsService = scope.ServiceProvider.GetRequiredService<CmsContentService>();
				byte[] body = ea.Body.ToArray();
				var message = Encoding.UTF8.GetString(body);
				var payload = System.Text.Json.JsonSerializer.Deserialize<EventPayload>(message);

				cmsService.UpdateCache(payload);
			}

			Console.WriteLine("Message received from rabbit mq");
			return Task.CompletedTask;
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			redisSubscriber?.Unsubscribe(redisChannel);

			if (rabbitChannel != null)
			{
				if (rabbitConsumer != null)
					rabbitConsumer.ReceivedAsync -= RabbitEventHandler;

				await rabbitConnection.CloseAsync(cancellationToken);
			}
		}

		public void Dispose()
		{
			redisConnection?.Dispose();
		}

	}

}