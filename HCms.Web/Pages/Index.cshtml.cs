using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace HCms.Web.Pages
{

	public class IndexModel : PageModel
	{
		public IActionResult OnGet()
		{
			if (!User.Identity.IsAuthenticated && !this.Request.Cookies.ContainsKey("X-JWT"))
				return Redirect("/auth");

			return Redirect("/documents");
		}
	}
}
