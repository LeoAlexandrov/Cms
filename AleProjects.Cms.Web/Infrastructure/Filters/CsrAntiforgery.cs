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
			var request = context.HttpContext.Request;

			if (request.Cookies.ContainsKey("X-JWT"))
			{
				// debugging

				bool valid;

				try
				{
					await _antiforgery.ValidateRequestAsync(context.HttpContext);
					valid = true;
				}
				catch (Exception ex)
				{
					valid = false;
					Console.WriteLine(ex.Message);
					Console.WriteLine(ex.StackTrace);
				}

				//bool valid = await _antiforgery.IsRequestValidAsync(context.HttpContext);

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
