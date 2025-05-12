using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using AleProjects.Cms.Infrastructure.Auth;


namespace AleProjects.Cms.Web.Pages.Auth
{

	[IgnoreAntiforgeryToken(Order = 1001)]
	public class GoogleModel(SignInHandler signInHandler) : PageModel
	{
		private readonly SignInHandler _signInHandler = signInHandler;

		public async Task<IActionResult> OnPost([FromForm] GoogleAuthPayload gPayload)
		{
			var popupAuth = !string.IsNullOrEmpty(this.Request.Cookies["popup_auth"]);
			var csrf = this.Request.Cookies["g_csrf_token"];

			this.Response.Cookies.Delete("ms_auth_state");
			this.Response.Cookies.Delete("github_auth_state");
			this.Response.Cookies.Delete("stackoverflow_auth_state");

			if (csrf != gPayload.G_Csrf_Token)
			{
				TempData["error"] = "csrf_forgery";
				return Redirect("/auth");
			}

			var login = await _signInHandler.Google(gPayload);

			switch (login.Status)
			{
				case LoginStatus.InvalidToken:
					TempData["error"] = "invalid_token";
					return Redirect("/auth");

				case LoginStatus.Forbidden:
					TempData["error"] = "forbidden";
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
