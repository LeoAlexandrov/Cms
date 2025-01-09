using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace AleProjects.Cms.Web.Pages
{
	public class SchemataModel : PageModel
	{
		public int SchemaId { get; set; }

		public IActionResult OnGet([FromRoute] int? id)
		{
			if (!User.Identity.IsAuthenticated && !this.Request.Cookies.ContainsKey("X-JWT"))
				return Redirect("/auth");

			SchemaId = id ?? 0;

			return Page();
		}
	}
}
