using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Claims;
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
				.AddScoped<ContentRepo>()
				.AddScoped<CmsContentService>();
		}

		public static IApplicationBuilder UseCmsContent(this IApplicationBuilder builder)
		{
			return builder.UseMiddleware<CmsContentMiddleware>();
		}
	}


	
	public class CmsContentMiddleware(RequestDelegate next)
	{
		private readonly RequestDelegate _next = next;

		private static void SetCulture(string lang)
		{
			if (!lang.StartsWith(Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName))
			{
				var docCulture = new CultureInfo(lang);
				Thread.CurrentThread.CurrentCulture = docCulture;
				Thread.CurrentThread.CurrentUICulture = docCulture;
			}
		}

		private static Task<bool> Authorize(ClaimsPrincipal user, string[] policies, IAuthorizationService authorizationService, bool allMust)
		{
			Task<bool> taskChain = Task.FromResult(allMust);

			foreach (var policy in policies)
			{
				taskChain = taskChain
					.ContinueWith(async previousTask =>
						{
							if (previousTask.Result ^ allMust)
								return !allMust;

							bool success;

							try
							{
								var result = await authorizationService.AuthorizeAsync(user, policy.Trim());
								success = result.Succeeded;
							}
							catch
							{
								success = false;
							}

							return success;
						})
					.Unwrap();
			}

			return taskChain;
		}

		private static async Task<bool> TryToAuthorize(ClaimsPrincipal user, string policies, IAuthorizationService authorizationService)
		{
			int commaIdx = policies.IndexOf(',');
			int semicolonIdx = policies.IndexOf(';');
			bool result;

			if (commaIdx == -1 && semicolonIdx == -1)
				result = await Authorize(user, [policies], authorizationService, true);
			else if (commaIdx == -1 || commaIdx > semicolonIdx)
				result = await Authorize(user, policies.Split(';'), authorizationService, false);
			else if (semicolonIdx == -1 || commaIdx < semicolonIdx)
				result = await Authorize(user, policies.Split(','), authorizationService, true);
			else
				result = false;

			return result;
		}

		public async Task InvokeAsync(HttpContext context, CmsContentService content, IMemoryCache cache, IAuthorizationService authService)
		{
			var routeData = context.GetRouteData();

			if (context.Request.Method == "GET" &&
				routeData.Values.TryGetValue("page", out object val) &&
				val.ToString() == "/Index")
			{
				string path = context.Request.Path.Value;

				if (cache.TryGetValue(path, out byte[] body))
				{
					Console.WriteLine("*** Cache hit ***");
					await context.Response.Body.WriteAsync(body);
				}
				else
				{
					var doc = await content.GetDocument(context.Request.Host.Value, path);

					if (doc == null)
					{
						context.Response.StatusCode = StatusCodes.Status404NotFound; 
						return;
					}

					SetCulture(doc.Language);

					if (doc.AuthRequired)
					{
						bool permit = await TryToAuthorize(context.User, doc.AuthPolicies, authService);

						if (!permit)
						{
							context.Response.StatusCode = StatusCodes.Status403Forbidden;
							return;
						}
					}

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

						if (!doc.AuthRequired &&
							(!context.Response.Headers.TryGetValue("Cache-Control", out var s) || s != "max-age=0, no-store"))
						{
#if !DEBUG
							cache.Set(path, body);
#endif
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

}