using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


using AleProjects.Cms.Application.Services;
using AleProjects.Cms.Sdk.ContentRepo;


var builder = WebApplication.CreateBuilder(args);

string settingsFile = builder.Configuration["Settings"];

if (!string.IsNullOrEmpty(settingsFile))
	builder.Configuration.AddJsonFile(settingsFile.StartsWith("../") ? Path.GetFullPath(settingsFile) : settingsFile);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddScoped<ContentRepo>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
