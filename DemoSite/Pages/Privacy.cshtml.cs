using System.Threading.Tasks;
using DemoSite.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DemoSite.Pages
{
	public class PrivacyModel(CmsContentService content) : PageModel
	{
		private readonly CmsContentService _content = content;

		public string HtmlContent { get; set; }

		public async Task<IActionResult> OnGet()
		{
			var doc = await _content.Repo.GetDocument("misc-docs", "/privacy", 0, false, true);

			if (doc == null)
				return NotFound();

			HtmlContent = doc.Fragments[0].Props.text;

			ViewData["HomePage"] = "/";
			ViewData["Language"] = doc.Language;
			ViewData["Title"] = doc.Title;
			ViewData["Theme"] = this.Request.Cookies["Theme"] ?? "light";

			return Page();
		}
	}

}
