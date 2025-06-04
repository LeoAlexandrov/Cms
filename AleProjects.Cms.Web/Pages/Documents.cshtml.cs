using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using AleProjects.Cms.Application.Services;


namespace AleProjects.Cms.Web.Pages
{

	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public class DocumentsModel(MediaManagementService mms) : PageModel
	{
		private readonly MediaManagementService _mms = mms;

		public int DocumentId { get; set; }

		public async Task<IActionResult> OnGet([FromRoute] string sId)
		{
			if (!User.Identity.IsAuthenticated && !this.Request.Cookies.ContainsKey("X-JWT"))
				return Redirect($"/auth/?backUrl={this.Request.Path}{this.Request.QueryString}");

			if (int.TryParse(sId, out int id))
			{
				DocumentId = id;
			}
			else if (string.IsNullOrEmpty(sId))
			{
				DocumentId = 0;
			}
			else if (sId.StartsWith("^('") && sId.EndsWith("')"))
			{
				var result = await _mms.Preview(sId[3..^2], null);

				return result.Type switch
				{
					ResultType.NotFound => NotFound(),
					ResultType.BadParameters => BadRequest(result.Errors),
					_ => PhysicalFile(result.Value.FullPath, result.Value.MimeType)
				};
			}
			else 
			{
				return NotFound();
			}

			return Page();
		}
	}
}
