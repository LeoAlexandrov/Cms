using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using AleProjects.Cms.Sdk.ContentRepo;
using AleProjects.Cms.Sdk.ViewModels;



namespace DemoSite.Pages
{
	public class IndexModel(ContentRepo repo) : PageModel
	{
		ContentRepo _repo = repo;

		public Document Document { get; set; }


		public async Task<IActionResult> OnGet()
		{
			if (this.Request.Path == "/666") // 500 page test
				_repo = null;

			this.Document = await _repo.GetDocument("home", this.Request.Path, 1, true);

			if (this.Document == null)
				return NotFound();

			if (this.Document.Attributes.ContainsKey("no-cache"))
				this.HttpContext.Response.Headers.Append("Cache-Control", "max-age=0, no-store");

			if (!string.IsNullOrEmpty(this.Document.Language))
				ViewData["Language"] = this.Document.Language;
			else
				ViewData["Language"] = "en";

			ViewData["Title"] = this.Document.Title;

			return Page();
		}
	}
}
