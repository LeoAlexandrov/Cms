using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using AleProjects.Base64;
using HCms.Application.Services;


namespace HCms.Web.Pages
{
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public class MediaModel(MediaManagementService ms) : PageModel
	{
		private readonly MediaManagementService _ms = ms;

		public string Link { get; set; }
		public long? MaxUploadSize { get; set; }
		public string SafeNameRegexString { get; set; }
		public object UploadParams { get; set; }
		public bool MediaPickerMode { get; set; }


		public async Task<IActionResult> OnGet([FromRoute] string link)
		{
			if (!User.Identity.IsAuthenticated && !this.Request.Cookies.ContainsKey("X-JWT"))
				return Redirect($"/auth/?backUrl={this.Request.Path}{this.Request.QueryString}");

			if (MediaPickerMode = this.Request.Query.ContainsKey("picker"))
				ViewData["NoNavigationDrawer"] = true;

			Link = link ?? string.Empty;

			if (string.IsNullOrEmpty(Link))
			{
				string redirLink = Base64Url.Encode(_ms.GetDefaultDisplayPlace() ?? string.Empty);

				if (!string.IsNullOrEmpty(redirLink))
					return Redirect($"/media/{redirLink}{(MediaPickerMode ? "?picker" : string.Empty)}");
			}

			var storageParams = _ms.GetCommonParams(Link);

			MaxUploadSize = storageParams.MaxUploadSize;
			SafeNameRegexString = storageParams.SafeNameRegex;
			UploadParams = new { MaxUploadSize, SafeNameRegexString };

			return Page();
		}

	}
}
