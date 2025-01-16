using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using AleProjects.Cms.Infrastructure.Auth;


namespace AleProjects.Cms.Web.Pages.Auth
{

	public class StackoverflowModel(SignInHandler signInHandler) : PageModel
	{
		private readonly SignInHandler _signInHandler = signInHandler;

		public async Task<IActionResult> OnGet([FromQuery] string code, [FromQuery] string state)
		{
			var popupAuth = !string.IsNullOrEmpty(this.Request.Cookies["popup_auth"]);
			var soState = this.Request.Cookies["stackoverflow_auth_state"];

			this.Response.Cookies.Delete("ms_auth_state");
			this.Response.Cookies.Delete("github_auth_state");
			this.Response.Cookies.Delete("stackoverflow_auth_state");

			if (state != soState)
			{
				TempData["error"] = "csrf_forgery";
				return Redirect("/auth");
			}

			var login = await _signInHandler.Stackoverflow(code,
				string.Format("https://{0}/auth/stackoverflow", this.Request.Host.Value),
				this.Request.Headers.UserAgent.First().ToString());

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

			return Redirect("/");
		}
	}
}