using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace HCms.Infrastructure.Notification
{
	public class EventDispatcher(Channel<NotificationEvent> channel, IHttpClientFactory httpClientFactory, ILogger<EventDispatcher> logger) : BackgroundService
	{
		readonly Channel<NotificationEvent> _channel = channel;
		readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
		readonly ILogger<EventDispatcher> _logger = logger;

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("EventDispatcher started.");

			var reader = _channel.Reader;

			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					var ev = await reader.ReadAsync(stoppingToken);
					await Dispatch(ev.Destinations, ev.Payload, _httpClientFactory, _logger);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error while processing event");
				}
			}

			_logger.LogInformation("EventDispatcher stopped.");
		}

		async static Task Dispatch(object[] destinations, EventPayload payload, IHttpClientFactory httpClientFactory, ILogger<EventDispatcher> logger)
		{
			HttpClient httpClient = null;

			foreach (var dest in destinations)
				switch (dest)
				{
					case WebhookDestination webhook:
						httpClient ??= httpClientFactory.CreateClient();
						await HttpSender.Send(httpClient, webhook, payload, logger);
						break;
					case RedisDestination redis:
						await RedisPublisher.Publish(redis, payload, logger);
						break;
					case RabbitMQDestination rabbit:
						await RabbitMQPublisher.Publish(rabbit, payload, logger);
						break;
				}

		}

	}

}