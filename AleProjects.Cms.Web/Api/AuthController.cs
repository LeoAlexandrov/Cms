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
		public async Task<IActionResult> Google([FromForm] GoogleAuthPayload gPayload)
		{
			var popupAuth = !string.IsNullOrEmpty(this.Request.Cookies["popup_auth"]);
			var csrf = this.Request.Cookies["g_csrf_token"];

			this.Response.Cookies.Delete("ms_auth_state");
			this.Response.Cookies.Delete("github_auth_state");
			this.Response.Cookies.Delete("stackoverflow_auth_state");

			if (csrf != gPayload.G_Csrf_Token)
				return Redirect("/auth?error=csrf_forgery");

			var login = await _signInHandler.Google(gPayload);

			switch (login.Status)
			{
				case LoginStatus.InvalidToken:
					return Redirect("/auth?error=invalid_token");

				case LoginStatus.Forbidden:
					return Redirect("/auth?error=forbidden");
			}

			SetAuthCookies(login.Jwt, login.Refresh, login.Locale);

			return Redirect(popupAuth ? "/auth?success=true" : "/");
		}

		[HttpGet]
		public async Task<IActionResult> Ms([FromQuery] string code, [FromQuery] string state, [FromQuery] string error)
		{
			var popupAuth = !string.IsNullOrEmpty(this.Request.Cookies["popup_auth"]);
			var msState = this.Request.Cookies["ms_auth_state"];

			this.Response.Cookies.Delete("ms_auth_state");
			this.Response.Cookies.Delete("github_auth_state");
			this.Response.Cookies.Delete("stackoverflow_auth_state");

			if (!string.IsNullOrEmpty(error))
				return Redirect("/auth?error=" + System.Net.WebUtility.UrlEncode(error));

			if (state != msState)
				return Redirect("/auth?error=csrf_forgery");

			var login = await _signInHandler.Microsoft(code, this.Request.Host.Value);

			switch (login.Status)
			{
				case LoginStatus.InvalidPayload:
					return Redirect("/auth?error=invalid_json");

				case LoginStatus.Forbidden:
					return Redirect("/auth?error=forbidden");
			}

			SetAuthCookies(login.Jwt, login.Refresh, login.Locale);

			return Redirect(popupAuth ? "/auth?success=true" : "/");
		}

		[HttpGet]
		public async Task<IActionResult> Github([FromQuery] string code, [FromQuery] string state, [FromQuery] string error)
		{
			var popupAuth = !string.IsNullOrEmpty(this.Request.Cookies["popup_auth"]);
			var ghState = this.Request.Cookies["github_auth_state"];

			this.Response.Cookies.Delete("ms_auth_state");
			this.Response.Cookies.Delete("github_auth_state");
			this.Response.Cookies.Delete("stackoverflow_auth_state");

			if (!string.IsNullOrEmpty(error))
				return Redirect("/auth?error=" + System.Net.WebUtility.UrlEncode(error));

			if (state != ghState)
				return Redirect("/auth?error=csrf_forgery");

			var login = await _signInHandler.Github(code, this.Request.Headers.UserAgent.First().ToString());

			switch (login.Status)
			{
				case LoginStatus.Forbidden:
					return Redirect("/auth?error=forbidden");
			}

			SetAuthCookies(login.Jwt, login.Refresh, login.Locale);

			return Redirect(popupAuth ? "/auth?success=true" : "/");
		}

		[HttpGet]
		public async Task<IActionResult> StackOverflow([FromQuery] string code, [FromQuery] string state)
		{
			var popupAuth = !string.IsNullOrEmpty(this.Request.Cookies["popup_auth"]);
			var soState = this.Request.Cookies["stackoverflow_auth_state"];

			this.Response.Cookies.Delete("ms_auth_state");
			this.Response.Cookies.Delete("github_auth_state");
			this.Response.Cookies.Delete("stackoverflow_auth_state");

			if (state != soState)
				return Redirect("/auth?error=csrf_forgery");

			var login = await _signInHandler.Stackoverflow(code, 
				string.Format("https://{0}/api/v1/auth/stackoverflow", this.Request.Host.Value),
				this.Request.Headers.UserAgent.First().ToString());

			switch (login.Status)
			{
				case LoginStatus.Forbidden:
					return Redirect("/auth?error=forbidden");
			}

			SetAuthCookies(login.Jwt, login.Refresh, login.Locale);

			return Redirect(popupAuth ? "/auth?success=true" : "/");
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
