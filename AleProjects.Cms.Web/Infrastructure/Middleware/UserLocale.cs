using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;


namespace AleProjects.Cms.Web.Infrastructure.Middleware
{
	public class UserLocale(RequestDelegate next)
	{
		private readonly RequestDelegate _next = next;

		public async Task Invoke(HttpContext context)
		{
			if (context.Request.Cookies.ContainsKey("X-Locale"))
			{
				string locale = context.Request.Cookies["X-Locale"];

				if (!string.IsNullOrEmpty(locale) &&
					!locale.StartsWith(Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName))
				{
					var userCulture = new CultureInfo(locale);

					Thread.CurrentThread.CurrentCulture = userCulture;
					Thread.CurrentThread.CurrentUICulture = userCulture;
				}

			}

			/* acceptable alternative way but not good as above

			if (context.User?.Identity != null && context.User.Identity.IsAuthenticated)
			{
				string locale = context.User.Claims.FirstOrDefault(c => c.Type == "locale")?.Value;

				if (!string.IsNullOrEmpty(locale) &&
					!locale.StartsWith(Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName))
				{
					var userCulture = new CultureInfo(locale);

					Thread.CurrentThread.CurrentCulture = userCulture;
					Thread.CurrentThread.CurrentUICulture = userCulture;
				}
			}
			*/

			await _next(context);
		}
	}
}
