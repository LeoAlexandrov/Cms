using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using HCms.ViewModels;
using DemoSite.Services;
using DemoSite.ViewModels;


namespace DemoSite.Pages
{
	public class IndexModel(CmsContentService content) : PageModel
	{
		private readonly CmsContentService _content = content;

		public Document Document { get; set; }
		public MainMenu MainMenu { get; set; }
		public NavigationMenu NavigationMenu { get; set; }
		public Footer Footer { get; set; }

		public string ChooseLayout()
		{
			string layout = this.Document.Parent == null || this.Document.Anchors == null ?
				"_Layout" :
				"_ScrollSpyLayout";

			return layout;
		}

		void InitializeFooter()
		{
			if (this.Document.Attributes.TryGetValue("footer", out string footer))
				this.Footer = System.Text.Json.JsonSerializer.Deserialize<Footer>(footer);
			else
				this.Footer = new Footer() { Links = [] };
		}


		public async Task<IActionResult> OnGet()
		{
			this.Document = _content.RequestedDocument;

			if (this.Document == null || !this.Document.ExactMatch)
				return NotFound();

			var authResult = await _content.Authorize(User);

			if (authResult != CmsContentService.AuthResult.Success)
				return new StatusCodeResult(403);

			if (this.Document.Attributes.ContainsKey("no-cache"))
				this.HttpContext.Response.Headers.Append("Cache-Control", "max-age=0, no-store");

			/*
			if (this.Document.Attributes.TryGetValue("nav-menu", out string menu))
				this.MainMenu = System.Text.Json.JsonSerializer.Deserialize<MainMenu>(menu);
			else
				this.MainMenu = new MainMenu() { Languages = [], Commands = [] };
			*/

			InitializeFooter();

			string lang = this.Document.Language;

			if (string.IsNullOrEmpty(lang))
				lang = "en-US";

			ViewData["HomePage"] = this.Document.Breadcrumbs[0].Path;
			ViewData["Language"] = lang;
			ViewData["Title"] = this.Document.Title;
			ViewData["Theme"] = this.Request.Cookies["Theme"] ?? "light";

			return Page();
		}
	}
}
