using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using HCms.ContentRepo;
using HCms.Routing;
using DemoSite.Services;


namespace DemoSite.Infrastructure.Middleware
{

	public static class CmsContentDIExtension
	{
		public static IServiceCollection AddCmsContent(this IServiceCollection services)
		{
			return services
				.AddTransient<IPathTransformer, DefaultPathTransformer>()
				.AddScoped<IContentRepo, SqlContentRepo>()
				.AddScoped<CmsContentService>();
		}

		public static IApplicationBuilder UseCmsContent(this IApplicationBuilder builder)
		{
			return builder.UseMiddleware<CmsContentMiddleware>();
		}
	}


	
	public class CmsContentMiddleware(RequestDelegate next)
	{
		readonly RequestDelegate _next = next;


		static void SetCulture(string lang)
		{
			if (!string.IsNullOrEmpty(lang) && 
				!lang.StartsWith(Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName))
			{
				var docCulture = new CultureInfo(lang);
				Thread.CurrentThread.CurrentCulture = docCulture;
				Thread.CurrentThread.CurrentUICulture = docCulture;
			}
		}

		static string Theme(HttpContext context)
		{
			return context.Request.Cookies["Theme"] ?? "light";
		}

		public async Task InvokeAsync(HttpContext context, CmsContentService content, IMemoryCache cache, IAuthorizationService authService)
		{
			var routeData = context.GetRouteData();

			if (context.Request.Method == "GET" &&
				routeData.Values.TryGetValue("page", out object val) &&
				val.ToString() == "/Index")
			{
				string path = ContentRepo.CleanPath(context.Request.Path.Value);
				string theme = Theme(context);
				string cacheKey = $"{theme}-{path}";

				if (cache.TryGetValue(cacheKey, out byte[] body))
				{
					Console.WriteLine($"*** Cache hit '{path}' ***");
					await context.Response.Body.WriteAsync(body);
				}
				else
				{
					int pageSize = 5;

					int position = context.Request.Query.TryGetValue("p", out var qp) && 
						int.TryParse(qp, out int p) && 
						p > 0 ? (p-1) * pageSize : 0;


					var doc = await content.GetDocument(context.Request.Host.Value, path, position, pageSize);

					SetCulture(doc?.Language);

					var originalBody = context.Response.Body;
					using var newBody = new MemoryStream();

					context.Response.Body = newBody;

					await _next(context);

					context.Response.Body = originalBody;

					newBody.Seek(0, SeekOrigin.Begin);
					body = new byte[newBody.Length];
					newBody.Read(body, 0, body.Length);

					if (context.Response.StatusCode == (int)HttpStatusCode.OK &&
						!doc.AuthRequired &&
						string.IsNullOrEmpty(context.Request.QueryString.Value) &&
						(!context.Response.Headers.TryGetValue("Cache-Control", out var s) || s != "max-age=0, no-store"))
					{
#if !DEBUG
						cache.Set(cacheKey, body);
#endif
						Console.WriteLine($"*** Cache add '{cacheKey}' ***");
					}

					await originalBody.WriteAsync(body);
				}
			}
			else
			{
				await _next(context);
			}
		}
	}

}