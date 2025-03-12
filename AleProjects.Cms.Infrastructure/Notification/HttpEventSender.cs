using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using AleProjects.Cms.Domain.ValueObjects;


namespace AleProjects.Cms.Infrastructure.Notification
{

	public static class HttpEventSender
	{
		public static async Task Send(HttpClient client, WebhookDestination destination, EventPayload payload)
		{
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
					Console.WriteLine($"Webhook destination '{destination.Endpoint}' failed with status: {response.StatusCode}");

			}
			catch (Exception ex)
			{
				Console.WriteLine($"Webhook destination '{destination.Endpoint}' failed with exception: {ex.Message}");
			}
		}
	}

}