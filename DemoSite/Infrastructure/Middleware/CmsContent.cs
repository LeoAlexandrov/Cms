﻿using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

using HCms.ContentRepo;
using HCms.Routing;

using DemoSite.Services;


namespace DemoSite.Infrastructure.Middleware
{

	public static class CmsContentDIExtension
	{
		struct MediaSettings
		{
			public string Host { get; set; }
			public string StoragePath { get; set; }
			public bool SelfServed { get; set; }
		}

		public static IServiceCollection AddCmsContent(this IServiceCollection services, string dbEngine, string connString, string mediaHost)
		{
			return services
				.AddSingleton<IPathTransformer, DefaultPathTransformer>(s => new DefaultPathTransformer(mediaHost))
				.AddCmsContentRepo<SqlContentRepo>(dbEngine, connString)
				.AddScoped<CmsContentService>();
		}

		public static IApplicationBuilder UseStaticCmsMedia(this IApplicationBuilder builder, IConfiguration mediaConfig)
		{
			IApplicationBuilder result;

			var settings = mediaConfig.Get<MediaSettings>();

			if (settings.SelfServed && 
				!string.IsNullOrEmpty(settings.StoragePath) &&
				!string.IsNullOrEmpty(settings.Host))
			{
				Uri uri = new(settings.Host[^1] == '/' ? settings.Host[..^1] : settings.Host);

				result = builder.UseStaticFiles(new StaticFileOptions
				{
					FileProvider = new PhysicalFileProvider(settings.StoragePath),
					RequestPath = uri.LocalPath
				});
			}
			else
			{
				result = builder;
			}

			return result;
		}

		public static IApplicationBuilder UseCmsContent(this IApplicationBuilder builder)
		{

			return builder.UseMiddleware<CmsContentMiddleware>();
		}
	}



	public class CmsContentMiddleware(RequestDelegate next, ILogger<CmsContentMiddleware> logger)
	{
		readonly RequestDelegate _next = next;
		readonly ILogger<CmsContentMiddleware> _logger = logger;


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

		static byte[] GZip(byte[] data)
		{
			using var ms = new MemoryStream();
			using var gzipStream = new GZipStream(ms, CompressionMode.Compress);
			
			gzipStream.Write(data, 0, data.Length);
			gzipStream.Flush();

			return ms.ToArray();
		}

		public async Task InvokeAsync(HttpContext context, CmsContentService content, IMemoryCache cache)
		{
			var routeData = context.GetRouteData();

			if (context.Request.Method == "GET" &&
				routeData.Values.TryGetValue("page", out object val) &&
				val is string sVal &&
				sVal == "/Index")
			{
				bool allowCaching = string.IsNullOrEmpty(context.Request.QueryString.Value);

				string path = ContentRepo.CleanPath(context.Request.Path.Value);
				string theme = Theme(context);
				string cacheKey = $"{theme}-{path}";

				/* We disabled in-process gzipping of cached content if we behind Cloudflare
				 * because it requires presense of Content-Length header in the response.
				 * This header may be removed by Nginx or other reverse proxy server, so
				 * Cloudflare will return 502 http status code.
				 * If you configure your reverse proxy to keep Content-Length header, 
				 * then you can set gzipCache to true.
				 */

				bool gzipCache = context.Request.Headers.All(h => !h.Key.StartsWith("cf-"));


				if (allowCaching && cache.TryGetValue(cacheKey, out byte[] body))
				{
					/* If the cache contains rendered body of the entire page,
					 * we return it immediately without calling the next middleware.
					 * Cached body may be gzipped.
					 */
#if DEBUG
					_logger.LogInformation("Cache hit '{cacheKey}'", cacheKey);
#endif
					if (gzipCache)
					{
						context.Response.Headers.ContentEncoding = "gzip";
						context.Response.Headers.ContentLength = body.Length;
						context.Response.Headers.ContentType = "text/html";
						context.Response.Headers.Vary = "Accept-Encoding";
					}

					await context.Response.Body.WriteAsync(body);
				}
				else
				{
					/* Otherwise we need to render the page. */

					CmsContentService.AwaitedResult awaitedResult = new();

					if (allowCaching) 
					{
						/* This branch of code prevents/minimizes 'cache stampede'.
						 * If some thread has already started rendering the page requested, 
						 * the 'CmsContentService.AwaitedResults' static concurrent dictionary
						 * will contain an element with the 'cacheKey' key.
						 * This element has CancellationToken which will be cancelled by that thread, 
						 * and byte array with the rendered page body.
						 * If no one has started rendering the page yet, 
						 * this thread adds its own element 'awaitedResult' to the dictionary,
						 * and manages the CancellationToken by itself.
						 */

						awaitedResult.Cts = new();

						var ar = CmsContentService.AwaitedResults.GetOrAdd(cacheKey, awaitedResult);

						if (ar.Body != null)
						{
							/* The page has been rendered by another thread, 
							 * because GetOrAdd above returned definitely different element with the 'Body' already set.
							 * No need to await, we can return it immediately. 
							 */

							awaitedResult.Cts.Dispose();

							if (gzipCache)
							{
								context.Response.Headers.ContentEncoding = "gzip";
								context.Response.Headers.ContentLength = ar.Body.Length;
								context.Response.Headers.ContentType = "text/html";
								context.Response.Headers.Vary = "Accept-Encoding";
							}

							await context.Response.Body.WriteAsync(ar.Body);
							return;
						}

						if (ar != awaitedResult)
						{
							/* The page is still being rendered by another thread
							 * because GetOrAdd above returned different element.
							 * We wait for 100 ms or until the CancellationToken 
							 * of the returned element is cancelled by the rendering thread.
							 */

							awaitedResult.Cts.Dispose();
							awaitedResult.Cts = null;

							try
							{
								await Task.Delay(100, ar.Cts.Token);
							}
							catch (TaskCanceledException)
							{
								/* The CancellationToken was cancelled by another thread.
								 * We check if the page has been cached testing the 'ar.Body' against null.
								 * 'ar.Body's and cached values are the same.
								 */

								if (ar.Body != null)
								{
									// Return rendered body immediately.

									if (gzipCache)
									{
										context.Response.Headers.ContentEncoding = "gzip";
										context.Response.Headers.ContentLength = ar.Body.Length;
										context.Response.Headers.ContentType = "text/html";
										context.Response.Headers.Vary = "Accept-Encoding";
									}

									await context.Response.Body.WriteAsync(ar.Body);
									return;
								}
							}
						}
					}

					/* If execution reached this point, it means that:
					 * - the page is not in the cache
					 * - the page is not being rendered by another thread
					 * - we added successfully our 'awaitedResult' to the dictionary
					 * - and we are the first thread to render the page.
					 */

					int pageSize = 5; // hardcoded for now, better to move it to the config file or to take it from query params

					int position = context.Request.Query.TryGetValue("p", out var qp) && 
						int.TryParse(qp, out int p) && 
						p > 0 ? (p-1) * pageSize : 0;


					var doc = await content.GetDocument(context.Request.Host.Value, path, position, pageSize, context.User);

					SetCulture(doc?.Language);

					var originalBody = context.Response.Body;
					using var newBody = new MemoryStream();

					context.Response.Body = newBody;

					await _next(context);

					context.Response.Body = originalBody;

					newBody.Seek(0, SeekOrigin.Begin);
					body = new byte[newBody.Length];
					newBody.Read(body, 0, body.Length);

					/* Now we have the entire page body in the 'body' variable
					 * which then will be written to the original response body and cached if possible.
					 * Caching is possible if:
					 * - the response status code is 200 OK,
					 * - the document is published (PublishStatus == 1),
					 * - the document is not protected by authorization,
					 * - the response does not contain Cache-Control header prohibiting caching.
					 */

					allowCaching &= context.Response.StatusCode == (int)HttpStatusCode.OK &&
						doc.PublishStatus == 1 &&
						!doc.AuthRequired &&
						(!context.Response.Headers.TryGetValue("Cache-Control", out var s) || s != "max-age=0, no-store");

					if (allowCaching)
					{
						/* GZipping (if allowed) and saving the body to the cache.
						 * Setting 'Body' for other threads awaiting for the page to be rendered.
						 */

#if !DEBUG
						var gzipped = gzipCache ? GZip(body) : body;

						cache.Set(cacheKey, awaitedResult.Body = gzipped);
#else
						_logger.LogInformation("Cached '{cacheKey}'", cacheKey);
#endif
					}

					if (awaitedResult.Cts != null)
					{
						/* Cancel the CancellationToken and 
						 * give a signal to awaiting threads that the page has been rendered. 
						 */

						awaitedResult.Cts.Cancel();
						CmsContentService.AwaitedResults.TryRemove(cacheKey, out _);
						awaitedResult.Cts.Dispose();
					}

					// Finally write the body to the response.

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