using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;


public static class ContentCacheExtension
{
	public static IApplicationBuilder UseContentCache(this IApplicationBuilder builder)
	{
		return builder.UseMiddleware<ContentCacheMiddleware>();
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
