using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

using AleProjects.Cms.Application.Services;


namespace AleProjects.Cms.Web.Pages.Auth
{

	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public class AuthModel(IConfiguration configuration, IHtmlLocalizer<SharedErrors> localizer) : PageModel
	{
		private readonly IConfiguration _configuration = configuration;
		private readonly IHtmlLocalizer<SharedErrors> _errorsLocalizer = localizer;

		public string CurrentSite { get; set; }
		public string GoogleClientId { get; set; }
		public string MicrosoftClientId { get; set; }
		public string GithubClientId { get; set; }
		public string StackOverflowClientId { get; set; }

		public string MicrosoftState { get; set; }
		public string GithubState { get; set; }
		public string StackOverflowState { get; set; }
		public string AuthError { get; set; }
		public bool PopupSuccess { get; set; }


		public static void SetAuthCookies(HttpResponse response, string jwt, string refresh, string locale)
		{
			response.Cookies.Append("X-JWT", jwt, new CookieOptions() { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax });
			response.Cookies.Append("X-Refresh", refresh, new CookieOptions() { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });

			if (!string.IsNullOrEmpty(locale))
				response.Cookies.Append("X-Locale", locale);
			else
				response.Cookies.Delete("X-Locale");

			response.Cookies.Delete("popup_auth");
		}

		public void OnGet([FromServices] UserManagementService ums)
		{
			if (ums.NoUsers())
			{
				this.Response.Redirect("/start");
				return;
			}

			string error = TempData["error"] as string;

			AuthError = error switch
			{
				"csrf_forgery" => (string)_errorsLocalizer.GetString("Auth_Csrf_Forgery"),
				"invalid_token" => (string)_errorsLocalizer.GetString("Auth_Invalid_Token"),
				"forbidden" => (string)_errorsLocalizer.GetString("Auth_Forbidden"),
				"invalid_json" => (string)_errorsLocalizer.GetString("Auth_Invalid_Json"),
				"access_denied" => string.Empty,
				_ => error,
			};

			bool success = TempData["popup"] as string == "success";

			if (string.IsNullOrEmpty(error) && success)
				PopupSuccess = true;

			CurrentSite = string.Format("https://{0}", this.Request.Host.Value);
			GoogleClientId = _configuration.GetValue<string>("Auth:Google:ClientId");
			MicrosoftClientId = _configuration.GetValue<string>("Auth:Microsoft:ClientId");
			GithubClientId = _configuration.GetValue<string>("Auth:Github:ClientId");
			StackOverflowClientId = _configuration.GetValue<string>("Auth:StackOverflow:ClientId");

			if (!string.IsNullOrEmpty(MicrosoftClientId))
			{
				MicrosoftState = RandomString.Create(32);
				this.Response.Cookies.Append("ms_auth_state", MicrosoftState);
			}

			if (!string.IsNullOrEmpty(GithubClientId))
			{
				GithubState =  RandomString.Create(32);
				this.Response.Cookies.Append("github_auth_state", GithubState);
			}

			if (!string.IsNullOrEmpty(StackOverflowClientId))
			{
				StackOverflowState = RandomString.Create(32);
				this.Response.Cookies.Append("stackoverflow_auth_state", StackOverflowState);
			}
		}

	}
}
