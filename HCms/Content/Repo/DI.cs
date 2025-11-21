using System;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using HCms.Infrastructure.Data;


namespace HCms.Content.Repo
{

	public static class ContentRepoDIExtension
	{
		public static IServiceCollection AddCmsContentRepo(this IServiceCollection services, Action<DbContextOptionsBuilder> setupAction)
		{
			services
				.AddDbContext<CmsDbContext>(setupAction)
				.AddScoped<IContentRepo, SqlContentRepo>();

			return services;
		}

		public static IServiceCollection AddCmsContentRepo(this IServiceCollection services, IConfiguration remoteRepoSection)
		{
			services
				.Configure<RemoteContentRepoOptions>(remoteRepoSection)
				.AddSingleton<IContentRepo, RemoteContentRepo>();

			return services;
		}

	}
}