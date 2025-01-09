using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using AleProjects.Cms.Application.Services;
using Microsoft.AspNetCore.Authorization;



namespace AleProjects.Cms.Web.Pages
{
	public class MediaModel(MediaManagementService mms, IAuthorizationService authService) : PageModel
	{
		private readonly MediaManagementService _mms = mms;
		private readonly IAuthorizationService _authService = authService;

		public string Link { get; set; }
		public int MaxUploadSize { get; set; }
		public bool UploadOnlySafeContent { get; set; }
		public string SafeNameRegexString { get; set; }
		public object UploadParams { get; set; }

		public async Task<IActionResult> OnGet([FromRoute] string link)
		{
			if (!User.Identity.IsAuthenticated && !this.Request.Cookies.ContainsKey("X-JWT"))
				return Redirect("/auth");

			Link = link ?? "";
			MaxUploadSize = _mms.MaxUploadSize;
			SafeNameRegexString = _mms.SafeNameRegexString;

			var authResult = await _authService.AuthorizeAsync(User, "UploadUnsafeContent");

			UploadOnlySafeContent = !authResult.Succeeded;
			UploadParams = new { MaxUploadSize, UploadOnlySafeContent, SafeNameRegexString };

			return Page();
		}

	}
}
