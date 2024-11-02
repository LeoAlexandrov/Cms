using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using AleProjects.Cms.Sdk.ContentRepo;
using AleProjects.Cms.Sdk.ViewModels;
using System.Threading.Tasks;

namespace DemoSite.Pages
{
	public class IndexModel : PageModel
	{
		ContentRepo _repo;

		public Document Document { get; set; }
		public IndexModel(ContentRepo repo)
		{
			_repo = repo;
		}

		public async Task<IActionResult> OnGet()
		{
			this.Document = await _repo.GetDocument("home1", null, true, true);

			return Page();

		}
	}
}
