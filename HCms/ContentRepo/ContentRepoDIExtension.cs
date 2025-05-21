using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using AleProjects.Cms.Infrastructure.Data;


namespace HCms.ContentRepo
{

	public static class ContentRepoDIExtension
	{
		static void ConfigureDatabase(DbContextOptionsBuilder options, string dbEngine, string connString)
		{
			if (string.IsNullOrEmpty(dbEngine) || dbEngine == "mssql")
				options.UseSqlServer(connString);
			else if (dbEngine == "postgres")
				options.UseNpgsql(connString);
			else if (dbEngine == "mysql")
				options.UseMySQL(connString);
			else
				throw new NotSupportedException($"Database engine '{dbEngine}' is not supported.");
		}

		public static IServiceCollection AddCmsContentRepo<T>(this IServiceCollection services, string dbEngine, string connString)
			where T : class, IContentRepo
		{
			services
				.AddDbContext<CmsDbContext>(options => ConfigureDatabase(options, dbEngine, connString))
				.AddScoped<IContentRepo, T>();

			return services;
		}
	}
}