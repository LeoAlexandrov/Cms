using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AleProjects.Cms.Web.Pages
{
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	[IgnoreAntiforgeryToken]
	public class NotFoundModel : PageModel
	{
		public NotFoundModel()
		{
		}

		public void OnGet()
		{
		}
	}

}
