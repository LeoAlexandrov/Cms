using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

using AleProjects.Random;
using HCms.Application.Services;
using HCms.Infrastructure.Auth;


namespace HCms.Web.Pages.Auth
{

	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	[IgnoreAntiforgeryToken(Order = 1001)]
	public class AuthModel(IOptions<AuthSettings> settings, IHtmlLocalizer<SharedErrors> localizer) : PageModel
	{
		private readonly AuthSettings _settings = settings.Value;
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

		public bool AllowAnonymous { get; set; }
		public string CfSiteKey { get; set; }


		public class AnonymousSignIn
		{
			[BindProperty(Name = "cf-turnstile-response")]
			[Required(AllowEmptyStrings = false)]
			public string CfTurnstileResponse { get; set; }
		}


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

			CurrentSite = $"https://{this.Request.Host.Value}";
			GoogleClientId = _settings.Google?.ClientId;
			MicrosoftClientId = _settings.Microsoft?.ClientId;
			GithubClientId = _settings.Github?.ClientId;
			StackOverflowClientId = _settings.StackOverflow?.ClientId;

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

			this.AllowAnonymous = _settings.DemoMode && ums.HasUser("demo", _settings.DefaultDemoModeRole);

			if (this.AllowAnonymous)
				this.CfSiteKey = _settings.CloudflareTT.SiteKey;

			if (this.Request.Query.ContainsKey("backUrl"))
				this.Response.Cookies.Append(
					"backUrl",
					this.Request.Query["backUrl"].ToString(),
					new CookieOptions()
					{
						HttpOnly = true,
						Secure = true,
						SameSite = SameSiteMode.Lax
					});
			else
				this.Response.Cookies.Delete("backUrl");
		}


		public IActionResult OnPost([Required] [FromForm] AnonymousSignIn model)
		{
			if (!ModelState.IsValid)
				return BadRequest();

			return Redirect($"/auth/anonymous?token={model.CfTurnstileResponse}");
		}
	}
}
