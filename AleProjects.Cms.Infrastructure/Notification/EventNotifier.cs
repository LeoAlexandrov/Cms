using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using AleProjects.Cms.Domain.Entities;
using AleProjects.Cms.Domain.ValueObjects;
using AleProjects.Cms.Infrastructure.Data;


namespace AleProjects.Cms.Infrastructure.Notification
{
	public interface IEventNotifier
	{
		Task Notify(string eventType, string root, string path, int id);
		Task Notify(string eventType, string[] fullPaths);
		Task Notify(string eventType, int destinationId = 0);
	}



	public class EventNotifier(CmsDbContext context, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory) : IEventNotifier
	{
		public const string EVENT_DOC_CREATE = "on_doc_create";
		public const string EVENT_DOC_CHANGE = "on_doc_change";
		public const string EVENT_DOC_UPDATE = "on_doc_update";
		public const string EVENT_DOC_DELETE = "on_doc_delete";
		public const string EVENT_MEDIA_CREATE = "on_media_create";
		public const string EVENT_MEDIA_DELETE = "on_media_delete";
		public const string EVENT_XMLSCHEMA = "on_xmlschema_change";
		public const string EVENT_ENABLE = "on_destination_enable";
		public const string EVENT_DISABLE = "on_destination_disable";

		readonly CmsDbContext _dbContext = context;
		readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
		readonly ILoggerFactory _loggerFactory = loggerFactory;


		async static Task PublishEvent(EventDestination[] destinations, EventPayload payload, HttpClient httpClient, ILogger<EventNotifier> logger)
		{
			foreach (var dest in destinations)
			{
				switch (dest.Type)
				{
					case "webhook":

						var webhook = System.Text.Json.JsonSerializer.Deserialize<WebhookDestination>(dest.Data);
						await HttpEventSender.Send(httpClient, webhook, payload, logger);

						break;

					case "redis":

						var redis = System.Text.Json.JsonSerializer.Deserialize<RedisDestination>(dest.Data);
						await RedisPublisher.Publish(redis, payload, logger);

						break;

					case "rabbitmq":

						var rabbit = System.Text.Json.JsonSerializer.Deserialize<RabbitMQDestination>(dest.Data);
						await RabbitMQPublisher.Publish(rabbit, payload, logger);

						break;
				}
			}
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
					(string.IsNullOrEmpty(d.TriggeringPath) && string.IsNullOrEmpty(d.TriggeringPathAux)) ||
					fullPath.StartsWith(d.TriggeringPath, StringComparison.OrdinalIgnoreCase) ||
					(!string.IsNullOrEmpty(d.TriggeringPathAux) && fullPath.StartsWith(d.TriggeringPathAux, StringComparison.OrdinalIgnoreCase))
					)
				.ToArray();

			var httpClient = dests.Any(d => d.Type == "webhook") ? _httpClientFactory.CreateClient() : null;

			var payload = new EventPayload()
			{
				Event = eventType,
				AffectedContent = [new() { Id = id, Root = root, Path = path }]
			};

			var logger = _loggerFactory?.CreateLogger<EventNotifier>();

			_ = PublishEvent(dests, payload, httpClient, logger)
				.ContinueWith(t => logger?.LogError(t.Exception, "Notification failed"), TaskContinuationOptions.OnlyOnFaulted);
		}

		public async Task Notify(string eventType, string[] fullPaths)
		{
			var all = await _dbContext.EventDestinations
				.AsNoTracking()
				.Where(d => d.Enabled)
				.ToArrayAsync();

			var dests = all
				.Where(d => 
					(string.IsNullOrEmpty(d.TriggeringPath) && string.IsNullOrEmpty(d.TriggeringPathAux)) ||
					fullPaths.Any(p =>
						p.StartsWith(d.TriggeringPath, StringComparison.OrdinalIgnoreCase) || 
						(!string.IsNullOrEmpty(d.TriggeringPathAux) && p.StartsWith(d.TriggeringPathAux, StringComparison.OrdinalIgnoreCase))
					))
				.ToArray();

			var httpClient = dests.Any(d => d.Type == "webhook") ? _httpClientFactory.CreateClient() : null;

			var payload = new EventPayload()
			{
				Event = eventType,
				AffectedContent = fullPaths.Select(p => new EventPayloadContentEntry() { Path = p }).ToArray()
			};

			var logger = _loggerFactory?.CreateLogger<EventNotifier>();

			_ = PublishEvent(dests, payload, httpClient, logger)
				.ContinueWith(t => logger?.LogError(t.Exception, "Notification failed"), TaskContinuationOptions.OnlyOnFaulted);
		}

		public async Task Notify(string eventType, int destinationId = 0)
		{
			var dests = destinationId != 0 ?
				await _dbContext.EventDestinations.AsNoTracking().Where(d => d.Id == destinationId).ToArrayAsync() :
				await _dbContext.EventDestinations.AsNoTracking().Where(d => d.Enabled).ToArrayAsync();

			var httpClient = dests.Any(d => d.Type == "webhook") ? _httpClientFactory.CreateClient() : null;

			var payload = new EventPayload() { Event = eventType };

			var logger = _loggerFactory?.CreateLogger<EventNotifier>();

			_ = PublishEvent(dests, payload, httpClient, logger)
				.ContinueWith(t => logger?.LogError(t.Exception, "Notification failed"), TaskContinuationOptions.OnlyOnFaulted);
		}
	}
}