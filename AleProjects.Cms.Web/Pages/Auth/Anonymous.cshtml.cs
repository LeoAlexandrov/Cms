using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using AleProjects.Cms.Infrastructure.Auth;


namespace AleProjects.Cms.Web.Pages.Auth
{

	public class Anonymous(SignInHandler signInHandler) : PageModel
	{
		private readonly SignInHandler _signInHandler = signInHandler;

		public async Task<IActionResult> OnGet([FromQuery] string token, [FromQuery] string error)
		{
			var popupAuth = !string.IsNullOrEmpty(this.Request.Cookies["popup_auth"]);

			this.Response.Cookies.Delete("github_auth_state");
			this.Response.Cookies.Delete("ms_auth_state");
			this.Response.Cookies.Delete("github_auth_state");
			this.Response.Cookies.Delete("stackoverflow_auth_state");

			if (!string.IsNullOrEmpty(error))
			{
				TempData["error"] = error;
				return Redirect("/auth");
			}

			string connectingIp = this.Request.Headers["Cf-Connecting-Ip"].FirstOrDefault();

			var login = await _signInHandler.Anonymous(token, connectingIp);

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
