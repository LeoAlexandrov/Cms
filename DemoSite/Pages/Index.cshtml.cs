using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using AleProjects.Cms.Sdk.ContentRepo;

namespace DemoSite.Pages
{
	public class IndexModel : PageModel
	{
		ContentRepo _repo;

		public IndexModel(ContentRepo repo)
		{
			_repo = repo;
		}

		public void OnGet()
		{
			var doc = _repo.GetDocument("home1", null, true, true).Result;
		}
	}
}
