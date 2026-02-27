using System;
using Microsoft.Extensions.DependencyInjection;


namespace HCms.Infrastructure.Media
{

	public static class MediaStorageDIExtension
	{
		public static IServiceCollection AddMediaStorage(this IServiceCollection services, Action<IServiceCollection> setupAction)
		{
			setupAction?.Invoke(services);

			return services;
		}
	}

}