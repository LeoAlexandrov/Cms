using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


using AleProjects.Cms.Sdk.ContentRepo;



void ConfigureServices(IServiceCollection services, ConfigurationManager configuration)
{
	string settingsFile = configuration["Settings"];

	if (!string.IsNullOrEmpty(settingsFile))
		configuration.AddJsonFile(settingsFile.StartsWith("../") ? Path.GetFullPath(settingsFile) : settingsFile);

	ContentCache.WebhookSecret = configuration["WebhookSecret"];

	services
		.AddMemoryCache()
		.AddTransient<IReferenceTransformer, DefaultReferenceTransformer>()
		.AddScoped<ContentRepo>()
		.AddRazorPages()
		.AddRazorPagesOptions(options => options.Conventions.AddPageRoute("/index", "{*url}"));
}


void ConfigureApp(WebApplication app)
{
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
		.UseContentCache()
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
