using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;

using AleProjects.Cms.Sdk.ContentRepo;


public static class ContentCacheExtension
{
	public static IApplicationBuilder UseContentCache(this IApplicationBuilder builder)
	{
		return builder.UseMiddleware<ContentCacheMiddleware>();
	}
}

public static class ContentCache
{
	public class Notification
	{
		public string Event { get; set; }
		public int AffectedDocument { get; set; }
		public string Secret { get; set; }
	}

	public static void Update(Notification model, IMemoryCache cache, ContentRepo repo)
	{
		if (model.Secret != "0tFFzJD1s652UIlnXzhOosmI2Z3HnI0r")
			return;

		if (model.Event == "on_xmlschema_change")
			ContentRepo.ReloadSchemata();
		
		if (cache is MemoryCache memoryCache)
			memoryCache.Clear();
	}
}


public class ContentCacheMiddleware(RequestDelegate next)
{
	private readonly RequestDelegate _next = next;

	public async Task InvokeAsync(HttpContext context, IMemoryCache cache)
	{
		var routeData = context.GetRouteData();

		if (context.Request.Method == "GET" && 
			routeData.Values.TryGetValue("page", out object val) && 
			val.ToString() == "/Index")
		{
			string path = context.Request.Path.ToString();

			if (cache.TryGetValue(path, out byte[] body))
			{
				Console.WriteLine("*** Cache hit ***");
				await context.Response.Body.WriteAsync(body);
			}
			else
			{
				var originalBody = context.Response.Body;
				using var newBody = new MemoryStream();

				context.Response.Body = newBody;

				await _next(context);

				context.Response.Body = originalBody;

				if (context.Response.StatusCode >= (int)HttpStatusCode.OK &&
					context.Response.StatusCode < (int)HttpStatusCode.Ambiguous)
				{
					newBody.Seek(0, SeekOrigin.Begin);
					body = new byte[newBody.Length];
					newBody.Read(body, 0, body.Length);

					if (!context.Response.Headers.TryGetValue("Cache-Control", out var s) || s != "max-age=0, no-store")
					{
						cache.Set(path, body);
						Console.WriteLine("*** Cache add ***");
					}

					await originalBody.WriteAsync(body);
				}
			}
		}
		else
		{
			await _next(context);
		}
	}
}
