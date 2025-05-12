using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using AleProjects.Cms.Infrastructure.Auth;


namespace AleProjects.Cms.Web.Pages.Auth
{

	public class GithubModel(SignInHandler signInHandler) : PageModel
	{
		private readonly SignInHandler _signInHandler = signInHandler;

		public async Task<IActionResult> OnGet([FromQuery] string code, [FromQuery] string state, [FromQuery] string error)
		{
			var popupAuth = !string.IsNullOrEmpty(this.Request.Cookies["popup_auth"]);
			var ghState = this.Request.Cookies["github_auth_state"];

			this.Response.Cookies.Delete("ms_auth_state");
			this.Response.Cookies.Delete("github_auth_state");
			this.Response.Cookies.Delete("stackoverflow_auth_state");

			if (!string.IsNullOrEmpty(error))
			{
				TempData["error"] = error;
				return Redirect("/auth");
			}

			if (state != ghState)
			{
				TempData["error"] = "csrf_forgery";
				return Redirect("/auth");
			}

			var login = await _signInHandler.Github(code, this.Request.Headers.UserAgent.First().ToString());

			switch (login.Status)
			{
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
