using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using HCms.ViewModels;
using DemoSite.Services;



namespace DemoSite.Pages
{
	public class IndexModel(CmsContentService content) : PageModel
	{
		private readonly CmsContentService _content = content;

		public Document Document { get; set; }

		public string ChooseLayout()
		{
			string layout = this.Document.Parent == null || this.Document.Anchors == null ?
				"_Layout" :
				"_ScrollSpyLayout";

			return layout;
		}

		public IActionResult OnGet()
		{
			this.Document = _content.RequestedDocument;

			if (this.Document.Attributes.ContainsKey("no-cache"))
				this.HttpContext.Response.Headers.Append("Cache-Control", "max-age=0, no-store");

			/*
			if (this.Document.Attributes.TryGetValue("nav-menu", out string menu))
				this.MainMenu = System.Text.Json.JsonSerializer.Deserialize<MainMenu>(menu);
			else
				this.MainMenu = new MainMenu() { Languages = [], Commands = [] };
			*/

			string lang = this.Document.Language;

			if (string.IsNullOrEmpty(lang))
				lang = "en-US";

			ViewData["HomePage"] = this.Document.Breadcrumbs[0].Path;
			ViewData["Language"] = lang;
			ViewData["Title"] = this.Document.Title;

			return Page();
		}
	}
}
