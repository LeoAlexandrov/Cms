using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

using AleProjects.Cms.Application.Services;


namespace AleProjects.Cms.Web.Pages
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


		public void OnGet([FromQuery] string error, [FromQuery] bool success, [FromServices] UserManagementService ums)
		{
			if (ums.NoUsers())
			{
				this.Response.Redirect("/start");
				return;
			}

			AuthError = error switch
			{
				"csrf_forgery" => (string)_errorsLocalizer.GetString("Auth_Csrf_Forgery"),
				"invalid_token" => (string)_errorsLocalizer.GetString("Auth_Invalid_Token"),
				"forbidden" => (string)_errorsLocalizer.GetString("Auth_Forbidden"),
				"invalid_json" => (string)_errorsLocalizer.GetString("Auth_Invalid_Json"),
				"access_denied" => string.Empty,
				_ => error,
			};

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
