using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using HCms.Infrastructure.Auth;


namespace HCms.Web.Pages.Auth
{

	public class MicrosoftModel(SignInHandler signInHandler) : PageModel
	{
		private readonly SignInHandler _signInHandler = signInHandler;

		public async Task<IActionResult> OnGet([FromQuery] string code, [FromQuery] string state, [FromQuery] string error)
		{
			var popupAuth = !string.IsNullOrEmpty(Request.Cookies["popup_auth"]);
			var msState = Request.Cookies["ms_auth_state"];

			Response.Cookies.Delete("ms_auth_state");
			Response.Cookies.Delete("github_auth_state");
			Response.Cookies.Delete("stackoverflow_auth_state");

			if (!string.IsNullOrEmpty(error))
			{
				TempData["error"] = error;
				return Redirect("/auth");
			}

			if (state != msState)
			{
				TempData["error"] = "csrf_forgery";
				return Redirect("/auth");
			}

			var login = await _signInHandler.Microsoft(code, Request.Host.Value);

			switch (login.Status)
			{
				case LoginStatus.InvalidPayload:
					TempData["error"] = "invalid_json";
					return Redirect("/auth");

				case LoginStatus.Forbidden:
					TempData["error"] = "forbidden";
					return Redirect("/auth");
			}

			AuthModel.SetAuthCookies(Response, login.Jwt, login.Refresh, login.Locale);

			if (popupAuth)
			{
				TempData["popup"] = "success";
				return Redirect("/auth");
			}

			string backUrl = Request.Cookies["backUrl"] ?? "/";
			Response.Cookies.Delete("backUrl");

			return Redirect(backUrl.StartsWith('/') ? backUrl : "/");
		}

	}
}
