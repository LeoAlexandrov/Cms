using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using HCms.Infrastructure.Auth;


namespace HCms.Web.Pages.Auth
{

	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	[IgnoreAntiforgeryToken(Order = 1001)]
	public class LdapModel(SignInHandler signInHandler) : PageModel
	{
		private readonly SignInHandler _signInHandler = signInHandler;


		public class LdapCredentials
		{
			public string Username { get; set; }
			public string Password { get; set; }
			public string State { get; set; }
		}


		public async Task<IActionResult> OnPost([Required] [FromForm] LdapCredentials credentials)
		{
			var popupAuth = !string.IsNullOrEmpty(this.Request.Cookies["popup_auth"]);
			var ldapState = this.Request.Cookies["ldap_auth_state"];

			this.Response.Cookies.Delete("ms_auth_state");
			this.Response.Cookies.Delete("github_auth_state");
			this.Response.Cookies.Delete("stackoverflow_auth_state");
			this.Response.Cookies.Delete("ldap_auth_state");

			if (credentials.State != ldapState)
			{
				TempData["error"] = "csrf_forgery";
				return Redirect("/auth");
			}

			var login = await _signInHandler.Ldap(credentials.Username, credentials.Password);

			switch (login.Status)
			{
				case LoginStatus.InvalidPayload:
				case LoginStatus.InvalidCredentials:
					TempData["error"] = "invalid_credentials";
					return Redirect("/auth");

				case LoginStatus.Forbidden:
					TempData["error"] = "forbidden";
					return Redirect("/auth");

				case LoginStatus.InternalError:
					TempData["error"] = "internal_error";
					return Redirect("/auth");
			}

			AuthModel.SetAuthCookies(this.Response, login.Jwt, login.Refresh, login.Locale);

			if (popupAuth)
			{
				TempData["popup"] = "success";
				return Redirect("/auth");
			}

			string backUrl = this.Request.Cookies["backUrl"] ?? "/";
			this.Response.Cookies.Delete("backUrl");

			return Redirect(backUrl.StartsWith('/') ? backUrl : "/");
		}
	}
}