using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;

using AleProjects.Cms.Application.Services;
using AleProjects.Cms.Infrastructure.Auth;
using AleProjects.Cms.Infrastructure.Data;
using AleProjects.Cms.Infrastructure.Media;
using AleProjects.Cms.Infrastructure.Notification;
using AleProjects.Cms.Web.Infrastructure.Auth;
using AleProjects.Cms.Web.Infrastructure.Middleware;
using AleProjects.Cms.Web.Infrastructure.MediaTypeFormatters;


void ConfigureDatabase(DbContextOptionsBuilder options, ConfigurationManager configuration)
{
	string dbEngine = configuration["DbEngine"];
	string connString = configuration.GetConnectionString("CmsDbConnection");

	if (string.IsNullOrEmpty(dbEngine) || dbEngine == "mssql")
		options.UseSqlServer(connString);
	else if (dbEngine == "postgres")
		options.UseNpgsql(connString);
	else if (dbEngine == "mysql")
		options.UseMySQL(connString);
	else
		throw new NotSupportedException($"Database engine '{dbEngine}' is not supported.");
}


void ConfigureServices(IServiceCollection services, ConfigurationManager configuration)
{
	string settingsFile = Environment.GetEnvironmentVariable("SETTINGS") ?? configuration["Settings"];

	if (!string.IsNullOrEmpty(settingsFile))
		configuration.AddJsonFile(settingsFile.StartsWith("../") ? Path.GetFullPath(settingsFile) : settingsFile, true);


	services
		.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
		.AddJwtBearer(options =>
			{
				options.Events = new JwtBearerEvents
				{
					OnMessageReceived = context =>
					{
						if (context.Request.Cookies.ContainsKey("X-JWT"))
							context.Token = context.Request.Cookies["X-JWT"];

						return Task.CompletedTask;
					}
				};

				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidIssuer = configuration.GetValue<string>("Auth:JwtIssuer"),
					ValidateAudience = true,
					ValidAudience = configuration.GetValue<string>("Auth:JwtAudience"),
					ValidateLifetime = true,
					ClockSkew = TimeSpan.Zero,
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(configuration.GetValue<string>("Auth:SecurityKey"))),
				};
			})
		.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", _ => { });


	services
		.AddDbContext<CmsDbContext>(options => ConfigureDatabase(options, configuration))
		.AddSingleton<FragmentSchemaRepo>(s =>
			{
				using var scope = s.CreateScope();
				var serviceProvider = scope.ServiceProvider;

				return new(
					serviceProvider.GetRequiredService<CmsDbContext>(), 
					serviceProvider.GetRequiredService<ILoggerFactory>());
			})
		.AddScoped<IEventNotifier, EventNotifier>()
		.AddScoped<ContentManagementService>()
		.Configure<LocalMediaStorageSettings>(configuration.GetSection("Media"))
		.AddTransient<IMediaStorage, LocalMediaStorage>()
		.AddTransient<MediaManagementService>()
		.AddTransient<SchemaManagementService>()
		.AddScoped<UserManagementService>()
		.AddScoped<EventDestinationsManagementService>()
		.Configure<AuthSettings>(configuration.GetSection("Auth"))
		.AddScoped<SignInHandler>()
		.AddSingleton<IRoleClaimPolicies, RoleClaimPolicies>()
		.AddScoped<IAuthorizationHandler, CanManageDocumentHandler>()
		.AddScoped<IAuthorizationHandler, CanManageFragmentHandler>()
		.AddScoped<IAuthorizationHandler, CanManageUserHandler>()
		.AddAuthorization(options =>
			{
				options.DefaultPolicy = new AuthorizationPolicyBuilder()
					.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, "ApiKey")
					.RequireAuthenticatedUser()
					.Build();

				RoleClaimPolicies.CreatePolicies(configuration, options);
				options.AddPolicy("CanManageDocument", policyBuilder => policyBuilder.AddRequirements(new CanManageDocumentRequirement()));
				options.AddPolicy("CanManageFragment", policyBuilder => policyBuilder.AddRequirements(new CanManageFragmentRequirement()));
				options.AddPolicy("CanManageUser", policyBuilder => policyBuilder.AddRequirements(new CanManageUserRequirement()));
			})
		.AddCors(options => options.AddPolicy("All", policyBuilder => policyBuilder.AllowAnyOrigin()))
		.AddAntiforgery(options => options.HeaderName = "X-RequestVerificationToken")
		.AddLocalization(options => options.ResourcesPath = "Resources")
		.AddDistributedMemoryCache()
		.AddHttpClient()
		.AddControllers(options =>
			{
				options.InputFormatters.Add(new MessagePackInputFormatter());
				options.OutputFormatters.Add(new MessagePackOutputFormatter());
			});

	services
		.AddApiVersioning()
		.AddApiExplorer(options =>
		 {
			 options.GroupNameFormat = "'v'VVV";
			 options.SubstituteApiVersionInUrl = true;
		 });

	services.AddRazorPages()
		.AddViewLocalization()
		.AddRazorRuntimeCompilation();
}


void ConfigureApp(WebApplication app)
{
	LocalMediaStorage.CheckAndCreateCacheFolder(app.Configuration);

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
		app.UseHsts(); // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	}


	app.UseForwardedHeaders()
		.UseStaticFiles()
		.UseRouting()
		.UseRequestLocalization(localizationOptions)
		.UseCors("All")
		.UseAuthentication()
		.UseAuthorization()
		.UseMiddleware<UserLocale>();


	app.MapControllers();

	app.UseStatusCodePagesWithReExecute("/Error/{0}");
	app.MapRazorPages();
}


var builder = WebApplication.CreateBuilder(args);

ConfigureServices(builder.Services, builder.Configuration);


var app = builder.Build();

ConfigureApp(app);

app.Run();