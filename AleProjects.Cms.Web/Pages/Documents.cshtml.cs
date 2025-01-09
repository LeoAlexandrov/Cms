using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace AleProjects.Cms.Web.Pages
{
	public class DocumentsModel : PageModel
	{
		public int DocumentId { get; set; }

		public IActionResult OnGet([FromRoute] int? id)
		{
			if (!User.Identity.IsAuthenticated && !this.Request.Cookies.ContainsKey("X-JWT"))
				return Redirect("/auth");

			DocumentId = id ?? 0;

			return Page();
		}
	}
}
