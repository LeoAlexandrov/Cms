using System;
using System.Globalization;
using System.IO;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using DemoSite.Infrastructure.Middleware;
using DemoSite.Services;



void ConfigureServices(IServiceCollection services, ConfigurationManager configuration)
{
	string settingsFile = configuration["Settings"];

	if (!string.IsNullOrEmpty(settingsFile))
		configuration.AddJsonFile(settingsFile.StartsWith("../") ? Path.GetFullPath(settingsFile) : settingsFile);


	services
		.AddMemoryCache()
		.AddCmsContent()
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
		});

	app.UseStatusCodePagesWithReExecute("/Error/{0}");
	app.MapRazorPages();
}


var builder = WebApplication.CreateBuilder(args);

ConfigureServices(builder.Services, builder.Configuration);


var app = builder.Build();

ConfigureApp(app);

app.Run();
