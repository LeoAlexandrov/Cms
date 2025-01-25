using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


using AleProjects.Base64;
using AleProjects.Cms.Domain.Entities;
using AleProjects.Cms.Infrastructure.Data;
using System.Net.Http.Json;



namespace AleProjects.Cms.Infrastructure.Notification
{

	public class WebhookNotification
	{
		public string Event { get; set; }
		public int AffectedDocument { get; set; }
		public string Secret { get; set; }
	}



	public interface IWebhookNotifier
	{
		Task Notify(string eventType, int affectedDocument, int wwebhook = 0);
	}



	public class WebhookNotifier(CmsDbContext context, IHttpClientFactory httpClientFactory) : IWebhookNotifier
	{
		public const string EVENT_CREATE = "on_doc_create";
		public const string EVENT_CHANGE = "on_doc_change";
		public const string EVENT_UPDATE = "on_doc_update";
		public const string EVENT_DELETE = "on_doc_delete";
		public const string EVENT_XMLSCHEMA = "on_xmlschema_change";
		public const string EVENT_ENABLE = "on_webhook_enable";
		public const string EVENT_DISABLE = "on_webhook_disable";

		private readonly CmsDbContext _dbContext = context;
		private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;


		struct Webhook
		{
			public string Endpoint;
			public string Secret;
		}

		private async static void Notify(HttpClient client, Webhook[] webhooks, string eventType, int affectedDocument)
		{
			foreach (var w in webhooks)
			{
				try
				{
					var r = await client.PostAsJsonAsync(
						w.Endpoint,
						new WebhookNotification()
						{
							Event = eventType,
							AffectedDocument = affectedDocument,
							Secret = w.Secret
						});

					if (!r.IsSuccessStatusCode)
						Console.WriteLine($"Webhook {w.Endpoint} failed with status {r.StatusCode}");

					r.Dispose();
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Webhook {w.Endpoint} failed with exception {ex.Message}");
				}
			}
		}

		public async Task Notify(string eventType, int affectedDocument, int webhook)
		{
			Webhook[] webhooks;

			if (eventType == EVENT_ENABLE || eventType == EVENT_DISABLE)
			{
				var w = await _dbContext.Webhooks.FindAsync(webhook);

				webhooks = w != null && w.Enabled ? [new Webhook() { Endpoint = w.Endpoint, Secret = w.Secret }] : [];
			}
			else if (eventType == EVENT_XMLSCHEMA || affectedDocument <= 0)
			{
				webhooks = await _dbContext.Webhooks
					.AsNoTracking()
					.Where(w => w.Enabled)
					.Select(w => new Webhook() { Endpoint = w.Endpoint, Secret = w.Secret })
					.ToArrayAsync();
			}
			else
			{
				var nodes = await _dbContext.DocumentPathNodes
					.AsNoTracking()
					.Where(n => n.DocumentRef == affectedDocument)
					.Select(n => n.Parent)
					.ToArrayAsync();

				webhooks = await _dbContext.Webhooks
					.AsNoTracking()
					.Where(w => (nodes.Contains(w.RootDocument) || w.RootDocument == affectedDocument || w.RootDocument == 0) && w.Enabled)
					.Select(w => new Webhook() { Endpoint = w.Endpoint, Secret = w.Secret })
					.ToArrayAsync();
			}

			if (webhooks.Length != 0)
				Task.Run(() => Notify(_httpClientFactory.CreateClient(), webhooks, eventType, affectedDocument));
		}
	}
}