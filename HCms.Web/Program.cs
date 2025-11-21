using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

using ProtoBuf.Grpc.Server;

using HCms.Application.Services;
using HCms.Content.Services;
using HCms.Infrastructure.Auth;
using HCms.Infrastructure.Data;
using HCms.Infrastructure.Media;
using HCms.Infrastructure.Notification;
using HCms.Web.Infrastructure.Auth;
using HCms.Web.Infrastructure.MediaTypeFormatters;
using HCms.Web.Infrastructure.Middleware;
using HCms.Web.Services;


void LoadConfiguration(ConfigurationManager configuration)
{
	string settingsFile = Environment.GetEnvironmentVariable("SETTINGS");

	if (!string.IsNullOrEmpty(settingsFile))
	{
		if (settingsFile.StartsWith("../"))
			settingsFile = Path.GetFullPath(settingsFile);

		if (File.Exists(settingsFile))
		{
			configuration.AddJsonFile(settingsFile, true);
			return;
		}
	}

	settingsFile = configuration["UseSettingsFile"];

	if (!string.IsNullOrEmpty(settingsFile))
		configuration.AddJsonFile(settingsFile.StartsWith("../") ? Path.GetFullPath(settingsFile) : settingsFile, true);
}

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
	var pmf = PathMapperFactory.Load(configuration);

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
		.AddTransient<IDbIndexConflictDetector, DbIndexConflictDetector>()
		.AddSingleton<IPathMapperFactory, PathMapperFactory>(s => pmf)
		.AddScoped<IEventNotifier, EventNotifier>()
		.AddScoped<ContentManagementService>()
		.AddScoped<ContentProvidingService>()
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
		.AddSingleton<IContentGrpcService, ContentGrpcService>()
		.AddControllers(options =>
			{
				options.InputFormatters.Add(new MessagePackInputFormatter());
				options.OutputFormatters.Add(new MessagePackOutputFormatter());
			});

	services
		.AddCodeFirstGrpc();

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
#if DEBUG
		// assumed production deployment configuration is the app running in docker container
		// behind a reverse proxy with TLS termination
		.UseHttpsRedirection()
#endif
		.UseStaticFiles()
		.UseRouting()
		.UseRequestLocalization(localizationOptions)
		.UseCors("All");
	
	app.UseAuthentication();
	app.UseAuthorization();
	app.UseMiddleware<UserLocale>();

	app.UseWhen(
		context => context.Request.Path.StartsWithSegments("/api"),
		appBuilder => appBuilder.UseEndpoints(e => e.MapControllers())
	);

	app.MapGrpcService<ContentGrpcService>();
	app.UseStatusCodePagesWithReExecute("/Error/{0}");
	app.MapRazorPages();
}



var builder = WebApplication.CreateBuilder(args);

LoadConfiguration(builder.Configuration);
ConfigureWebHost(builder.WebHost);
ConfigureServices(builder.Services, builder.Configuration);


var app = builder.Build();

ConfigureApp(app);

app.Run();