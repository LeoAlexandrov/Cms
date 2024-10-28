using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;



namespace AleProjects.Cms.Web.Infrastructure.Filters
{

	public class CsrAntiforgeryFilter(IAntiforgery antiforgery) : IAsyncActionFilter
	{
		private readonly IAntiforgery _antiforgery = antiforgery;

		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
			HttpRequest request = context.HttpContext.Request;
			string method = request.Method;

			if (method != "GET" && method != "HEAD" && method != "TRACE" && method != "OPTIONS" && request.Cookies.ContainsKey("X-JWT"))
			{
				bool valid = await _antiforgery.IsRequestValidAsync(context.HttpContext);

				if (!valid)
				{
					var response = context.HttpContext.Response;

					response.ContentType = "application/problem+json";
					response.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;

					await response.WriteAsJsonAsync(new
						{
							title = "Antiforgery token is not valid",
							status = (int)System.Net.HttpStatusCode.BadRequest,
							errors = new { antiforgery_token = new string[] { "token is not valid" } },
							traceId = request.HttpContext.TraceIdentifier
						});

					return;
				}
			}

			await next();
		}
	}



	public class CsrAntiforgeryAttribute : TypeFilterAttribute
	{
		public CsrAntiforgeryAttribute() : base(typeof(CsrAntiforgeryFilter)) { }
	}


}
