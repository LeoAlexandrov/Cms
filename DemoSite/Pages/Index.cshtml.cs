using System;
using System.Globalization;
using System.Threading;
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

		public string ChooseLayout()
		{
			string layout = this.Document.Parent == null || this.Document.Anchors == null ?
				"_Layout" :
				"_ScrollSpyLayout";

			return layout;
		}

		public void SetPageLocale()
		{
			string lang = this.Document.Language;

			if (!lang.StartsWith(Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName))
			{
				var docCulture = new CultureInfo(lang);
				Thread.CurrentThread.CurrentCulture = docCulture;
				Thread.CurrentThread.CurrentUICulture = docCulture;
			}
		}

		public async Task<IActionResult> OnGet()
		{
			if (this.Request.Path == "/666") // 500 page test
				_repo = null;

			this.Document = await _repo.GetDocument("home", this.Request.Path.Value, 0, true);
			//this.Document = this.HttpContext.Features.Get<Document>();

			if (this.Document == null)
				return NotFound();

			if (this.Document.Attributes.ContainsKey("no-cache"))
				this.HttpContext.Response.Headers.Append("Cache-Control", "max-age=0, no-store");

			string lang = this.Document.Language;

			if (string.IsNullOrEmpty(lang))
				lang = "en-US";

			ViewData["Language"] = lang;
			ViewData["Title"] = this.Document.Title;

			return Page();
		}
	}
}
