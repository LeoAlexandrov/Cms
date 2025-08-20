using System;
using System.Globalization;
using System.IO;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using DemoSite.Infrastructure.Middleware;
using DemoSite.Services;


void ConfigureWebHost(IWebHostBuilder webHostBuilder)
{
	/*
	 * Kestrel configuration assumes "Kestrel" section in appsettings.json
	 * https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-8.0
	 * ----
		"Kestrel": {
			"Endpoints": {
				"Http": {
					"Url": "http://localhost:8085"
				}
			}
		},
	 * ----
	 */

	webHostBuilder.ConfigureKestrel((context, options) =>
		options.Configure(context.Configuration.GetSection("Kestrel")));

}

void ConfigureServices(IServiceCollection services, ConfigurationManager configuration)
{
	string settingsFile = Environment.GetEnvironmentVariable("SETTINGS") ?? configuration["Settings"];

	if (!string.IsNullOrEmpty(settingsFile))
		configuration.AddJsonFile(settingsFile.StartsWith("../") ? Path.GetFullPath(settingsFile) : settingsFile, true);


	services
		.AddMemoryCache()
		.AddCmsContent(
			configuration["DbEngine"], 
			configuration.GetConnectionString("CmsDbConnection"),
			configuration["Media:Host"])
		.AddLocalization(options => options.ResourcesPath = "Resources")
		.AddHostedService<EventSubscriptionService>()
		.AddRazorPages(options => options.Conventions.AddPageRoute("/index", "{*url}"))
		.AddViewLocalization();
}

void ConfigureApp(WebApplication app)
{
	var supportedCultures = new[]
	{
		new CultureInfo("en"),
		new CultureInfo("fr")
	};

	var localizationOptions = new RequestLocalizationOptions
	{
		DefaultRequestCulture = new RequestCulture("en"),
		SupportedCultures = supportedCultures,
		SupportedUICultures = supportedCultures
	};


	if (app.Environment.IsDevelopment())
	{
		app.UseDeveloperExceptionPage();
	}
	else
	{
		app.UseExceptionHandler("/Error");
		// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
		app.UseHsts();
	}


	app.UseHttpsRedirection()
		.UseStaticFiles()
		.UseStaticCmsMedia(app.Configuration.GetSection("Media"))
		.UseRouting()
		.UseRequestLocalization(localizationOptions)
		.UseAuthentication()
		.UseAuthorization()
		.UseCmsContent();

	app.MapPost("/cms-webhook-handler",
		(HCms.Dto.EventPayload model, CmsContentService cmsService, IConfiguration configuration, HttpRequest request) =>
		{
			string secret = configuration["Webhook:Secret"];

			if (secret == request.Headers["X-Secret"])
				cmsService.UpdateCache(model);

			return Results.NoContent();
		});

	app.UseStatusCodePagesWithReExecute("/Error/{0}");
	app.MapRazorPages();
}


var builder = WebApplication.CreateBuilder(args);

ConfigureWebHost(builder.WebHost);
ConfigureServices(builder.Services, builder.Configuration);


var app = builder.Build();

ConfigureApp(app);

app.Run();