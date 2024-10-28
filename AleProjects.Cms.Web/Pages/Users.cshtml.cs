using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AleProjects.Cms.Web.Pages
{
	public class UsersModel : PageModel
	{
		public int UserId { get; set; }

		public IActionResult OnGet([FromRoute] int? id)
		{
			if (!User.Identity.IsAuthenticated && !this.Request.Cookies.ContainsKey("X-JWT"))
				return Redirect("/auth");

			UserId = id ?? 0;

			return Page();
		}
	}
}