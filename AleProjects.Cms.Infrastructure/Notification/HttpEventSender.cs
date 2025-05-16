using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using AleProjects.Cms.Domain.ValueObjects;


namespace AleProjects.Cms.Infrastructure.Notification
{

	public static class HttpEventSender
	{
		public static async Task Send(HttpClient client, WebhookDestination destination, EventPayload payload, ILogger<EventNotifier> logger)
		{
			if (client == null)
			{
				logger?.LogError("HttpClient for webhook destination '{Endpoint}' is null", destination.Endpoint);
				return;
			}

			using HttpRequestMessage request = new()
			{
				Method = HttpMethod.Post,
				RequestUri = new Uri(destination.Endpoint),
				Content = JsonContent.Create(payload)
			};

			request.Headers.Add("X-Secret", destination.Secret);

			try
			{
				using HttpResponseMessage response = await client.SendAsync(request);

				if (!response.IsSuccessStatusCode)
					logger?.LogError("Webhook destination '{Endpoint}' failed with status: {StatusCode}", destination.Endpoint, response.StatusCode);

			}
			catch (Exception ex)
			{
				logger?.LogError(ex, "Webhook destination '{Endpoint}' failed with exception: {Message}", destination.Endpoint, ex.Message);
			}
		}
	}

}