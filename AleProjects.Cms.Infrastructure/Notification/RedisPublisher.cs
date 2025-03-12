using System;
using System.Threading.Tasks;

using StackExchange.Redis;

using AleProjects.Endpoints;
using AleProjects.Cms.Domain.ValueObjects;


namespace AleProjects.Cms.Infrastructure.Notification
{

	public static class RedisPublisher
	{
		public static Task<long> Publish(RedisDestination destination, EventPayload payload)
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

			Task<long> result;

			try
			{
				ConnectionMultiplexer connection = ConnectionMultiplexer.Connect(opts);

				var channel = RedisChannel.Literal(destination.Channel);
				var pubsub = connection.GetSubscriber();
				string message = System.Text.Json.JsonSerializer.Serialize(payload);

				result = pubsub.PublishAsync(channel, message, CommandFlags.FireAndForget);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Redis destination '{destination.Endpoint}' failed with exception: {ex.Message}");
				result = Task.FromResult(-1L);
			}

			return result;
		}
	}
}