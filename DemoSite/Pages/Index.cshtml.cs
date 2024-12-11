using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using AleProjects.Cms.Sdk.ContentRepo;
using AleProjects.Cms.Sdk.ViewModels;
using System.Threading.Tasks;
using System;

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
			if (this.Request.Path == "/666") // 500 page test
				_repo = null;

			this.Document = await _repo.GetDocument("home", this.Request.Path, true, true);

			if (this.Document == null)
			{
				return NotFound();
			}

			return Page();
		}
	}
}
