using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace AleProjects.Cms.Web.Pages
{

	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public class SchemataModel : PageModel
	{
		public int SchemaId { get; set; }

		public IActionResult OnGet([FromRoute] int? id)
		{
			if (!User.Identity.IsAuthenticated && !this.Request.Cookies.ContainsKey("X-JWT"))
				return Redirect($"/auth/?backUrl={this.Request.Path}{this.Request.QueryString}");

			SchemaId = id ?? 0;

			return Page();
		}
	}
}
