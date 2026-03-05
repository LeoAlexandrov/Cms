using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using HCms.Domain.Entities;
using HCms.Infrastructure.Data;


namespace HCms.Infrastructure.Notification
{
	public interface IEventNotifier
	{
		Task Notify(string eventType, string root, string path, int id);
		Task Notify(string eventType, string[] fullPaths);
		Task Notify(string eventType, int destinationId = 0);
	}



	public class EventNotifier(CmsDbContext context, Channel<NotificationEvent> channel) : IEventNotifier
	{
		public const string EVENT_DOC_CREATE = "on_doc_create";
		public const string EVENT_DOC_CHANGE = "on_doc_change";
		public const string EVENT_DOC_UPDATE = "on_doc_update";
		public const string EVENT_DOC_DELETE = "on_doc_delete";
		public const string EVENT_MEDIA_CREATE = "on_media_create";
		public const string EVENT_MEDIA_DELETE = "on_media_delete";
		public const string EVENT_USERS_CHANGE = "on_users_change";
		public const string EVENT_XMLSCHEMA = "on_xmlschema_change";
		public const string EVENT_ENABLE = "on_destination_enable";
		public const string EVENT_DISABLE = "on_destination_disable";

		readonly CmsDbContext _dbContext = context;
		readonly Channel<NotificationEvent> _channel = channel;


		static object[] Destinations(EventDestination[] destinations)
		{
			object[] result = new object[destinations.Length];
			int i = 0;

			foreach (var dest in destinations)
				switch (dest.Type)
				{
					case "webhook":
						result[i++] = System.Text.Json.JsonSerializer.Deserialize<WebhookDestination>(dest.Data);
						break;

					case "redis":
						result[i++] = System.Text.Json.JsonSerializer.Deserialize<RedisDestination>(dest.Data);
						break;

					case "rabbitmq":
						result[i++] = System.Text.Json.JsonSerializer.Deserialize<RabbitMQDestination>(dest.Data);
						break;
				}

			return result;
		}

		public async Task Notify(string eventType, string root, string path, int id)
		{
			var all = await _dbContext.EventDestinations
				.AsNoTracking()
				.Where(d => d.Enabled)
				.ToArrayAsync();

			string fullPath = $"{root}/{path}";

			var dests = all
				.Where(d =>
					string.IsNullOrEmpty(d.TriggeringPath) && string.IsNullOrEmpty(d.TriggeringPathAux) ||
					fullPath.StartsWith(d.TriggeringPath, StringComparison.OrdinalIgnoreCase) ||
					!string.IsNullOrEmpty(d.TriggeringPathAux) && fullPath.StartsWith(d.TriggeringPathAux, StringComparison.OrdinalIgnoreCase)
					)
				.ToArray();

			var ev = new NotificationEvent()
			{
				Destinations = Destinations(dests),
				Payload = new EventPayload()
				{
					Event = eventType,
					AffectedContent = [new() { Id = id, Root = root, Path = path }]
				}
			};

			await _channel.Writer.WriteAsync(ev);
		}

		public async Task Notify(string eventType, string[] fullPaths)
		{
			var all = await _dbContext.EventDestinations
				.AsNoTracking()
				.Where(d => d.Enabled)
				.ToArrayAsync();

			var dests = all
				.Where(d => 
					string.IsNullOrEmpty(d.TriggeringPath) && string.IsNullOrEmpty(d.TriggeringPathAux) ||
					fullPaths.Any(p =>
						p.StartsWith(d.TriggeringPath, StringComparison.OrdinalIgnoreCase) || 
						!string.IsNullOrEmpty(d.TriggeringPathAux) && p.StartsWith(d.TriggeringPathAux, StringComparison.OrdinalIgnoreCase)
					))
				.ToArray();

			var ev = new NotificationEvent()
			{
				Destinations = Destinations(dests),
				Payload = new EventPayload()
				{
					Event = eventType,
					AffectedContent = [.. fullPaths.Select(p => new EventPayloadEntry() { Path = p })]
				}
			};

			await _channel.Writer.WriteAsync(ev);
		}

		public async Task Notify(string eventType, int destinationId = 0)
		{
			var dests = destinationId != 0 ?
				await _dbContext.EventDestinations.AsNoTracking().Where(d => d.Id == destinationId).ToArrayAsync() :
				await _dbContext.EventDestinations.AsNoTracking().Where(d => d.Enabled).ToArrayAsync();

			var ev = new NotificationEvent()
			{
				Destinations = Destinations(dests),
				Payload = new EventPayload() { Event = eventType }
			};

			await _channel.Writer.WriteAsync(ev);
		}
	}
}