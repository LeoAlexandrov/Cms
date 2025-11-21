using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

using Asp.Versioning;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using HCms.Infrastructure.Auth;


namespace HCms.Web.Api
{

	[Route("api/v{version:apiVersion}/[controller]/{action}")]
	[ApiVersion("1.0")]
	[ApiController]
	public class AuthController(SignInHandler signInHandler, IAntiforgery antiforgery) : ControllerBase
	{
		readonly SignInHandler _signInHandler = signInHandler;
		readonly IAntiforgery _antiforgery = antiforgery;


		void SetAuthCookies(string jwt, string refresh, string locale)
		{
			Response.Cookies.Append("X-JWT", jwt, new CookieOptions() { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax });
			Response.Cookies.Append("X-Refresh", refresh, new CookieOptions() { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });

			if (!string.IsNullOrEmpty(locale))
				Response.Cookies.Append("X-Locale", locale);
			else
				Response.Cookies.Delete("X-Locale");

			Response.Cookies.Delete("popup_auth");
		}

		[HttpPost]
		public IActionResult Signout()
		{
			string refresh = Request.Cookies["X-Refresh"];

			Response.Cookies.Delete("X-JWT");
			Response.Cookies.Delete("X-Refresh");

			_signInHandler.SignOut(refresh);

			return Ok();
		}

		[HttpPost]
		public async Task<IActionResult> Refresh()
		{
			string refresh = Request.Cookies["X-Refresh"];

			if (string.IsNullOrEmpty(refresh))
				return BadRequest();

			var login = await _signInHandler.Refresh(refresh);

			switch (login.Status)
			{
				case LoginStatus.Forbidden:
				case LoginStatus.Expiration:
				case LoginStatus.NotFound:
					return BadRequest();

				case LoginStatus.IsValid:
					return NoContent();

				case LoginStatus.InternalError:
					return StatusCode((int)HttpStatusCode.InternalServerError);
			}

			SetAuthCookies(login.Jwt, login.Refresh, login.Locale);

			ClaimsIdentity identity = new(login.Claims, login.AuthenticationType);
			HttpContext.User = new(identity);

			var tokens = _antiforgery.GetAndStoreTokens(HttpContext);

			return Ok(new { CsrfToken = tokens.RequestToken });
		}

	}
}
