using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace AleProjects.Cms.Web.Pages
{

	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public class EventsModel : PageModel
	{
		public IActionResult OnGet()
		{
			if (!User.Identity.IsAuthenticated && !this.Request.Cookies.ContainsKey("X-JWT"))
				return Redirect("/auth");

			return Page();
		}
	}
}
