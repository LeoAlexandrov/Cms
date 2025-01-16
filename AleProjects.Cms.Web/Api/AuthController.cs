using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

using AleProjects.Cms.Infrastructure.Auth;



namespace AleProjects.Cms.Web.Api
{

	[Route("api/v{version:apiVersion}/[controller]/{action}")]
	[ApiVersion("1.0")]
	[ApiController]
	public class AuthController(SignInHandler signInHandler) : ControllerBase
	{
		private readonly SignInHandler _signInHandler = signInHandler;


		private void SetAuthCookies(string jwt, string refresh, string locale)
		{
			this.Response.Cookies.Append("X-JWT", jwt, new CookieOptions() { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax });
			this.Response.Cookies.Append("X-Refresh", refresh, new CookieOptions() { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });

			if (!string.IsNullOrEmpty(locale))
				this.Response.Cookies.Append("X-Locale", locale);
			else
				this.Response.Cookies.Delete("X-Locale");

			this.Response.Cookies.Delete("popup_auth");
		}

		[HttpPost]
		public IActionResult Signout()
		{
			string refresh = this.Request.Cookies["X-Refresh"];

			this.Response.Cookies.Delete("X-JWT");
			this.Response.Cookies.Delete("X-Refresh");

			_signInHandler.SignOut(refresh);

			return Ok();
		}

		[HttpPost]
		public async Task<IActionResult> Refresh()
		{
			string refresh = this.Request.Cookies["X-Refresh"];

			if (string.IsNullOrEmpty(refresh))
				return BadRequest();

			var login = await _signInHandler.Refresh(refresh);

			switch (login.Status)
			{
				case LoginStatus.Forbidden:
				case LoginStatus.Expiration:
					return BadRequest();

				case LoginStatus.InternalError:
					return StatusCode((int)HttpStatusCode.InternalServerError);
			}

			SetAuthCookies(login.Jwt, login.Refresh, login.Locale);

			return Ok();
		}

	}
}
