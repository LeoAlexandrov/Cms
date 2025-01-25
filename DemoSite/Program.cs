using System;
using System.Globalization;
using System.IO;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using AleProjects.Cms.Sdk.ContentRepo;
using AleProjects.Cms.Sdk.Routing;



void ConfigureServices(IServiceCollection services, ConfigurationManager configuration)
{
	string settingsFile = configuration["Settings"];

	if (!string.IsNullOrEmpty(settingsFile))
		configuration.AddJsonFile(settingsFile.StartsWith("../") ? Path.GetFullPath(settingsFile) : settingsFile);

	ContentCache.WebhookSecret = configuration["WebhookSecret"];

	services
		.AddMemoryCache()
		.AddTransient<IPathTransformer, DefaultPathTransformer>()
		.AddScoped<ContentRepo>()
		.AddLocalization(options => options.ResourcesPath = "Resources")
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


	app.UseStatusCodePagesWithReExecute("/{0}");

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
#if !DEBUG
		.UseContentCache()
#endif
		.UseAuthorization();

	app.MapPost("/cms-webhook-handler",
		async (ContentCache.Notification model, IMemoryCache cache, ContentRepo repo) => await ContentCache.Update(model, cache, repo));
		

	app.MapRazorPages();
}



var builder = WebApplication.CreateBuilder(args);

ConfigureServices(builder.Services, builder.Configuration);


var app = builder.Build();

ConfigureApp(app);

app.Run();
