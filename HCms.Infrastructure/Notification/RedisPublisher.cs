using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using StackExchange.Redis;

using AleProjects.Endpoints;


namespace HCms.Infrastructure.Notification
{

	public static class RedisPublisher
	{
		public static async Task<long> Publish(RedisDestination destination, EventPayload payload, ILogger<EventDispatcher> logger)
		{
			ConfigurationOptions opts = new()
			{
				EndPoints = [.. EndPoints.Parse(destination.Endpoint)],
			};

			if (!string.IsNullOrEmpty(destination.User))
			{
				opts.User = destination.User;

				if (!string.IsNullOrEmpty(destination.Password))
					opts.Password = destination.Password;
			}

			long result;

			try
			{
				var connection = await ConnectionMultiplexer.ConnectAsync(opts);
				var channel = RedisChannel.Literal(destination.Channel);
				var pubsub = connection.GetSubscriber();
				string message = System.Text.Json.JsonSerializer.Serialize(payload);

				result = await pubsub.PublishAsync(channel, message, CommandFlags.FireAndForget);
			}
			catch (Exception ex)
			{
				logger?.LogError(ex, "Redis destination '{Endpoint}' failed with exception: {Message}", destination.Endpoint, ex.Message);
				result = -1L;
			}

			return result;
		}
	}
}