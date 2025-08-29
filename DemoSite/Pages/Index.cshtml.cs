using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using DemoSite.Services;
using DemoSite.ViewModels;


namespace DemoSite.Pages
{
	public class IndexModel(CmsContentService content) : PageModel
	{
		readonly CmsContentService _content = content;

		public CmsPageModel CmsPage { get; set; }


		public string ChooseLayout()
		{
			string layout = CmsPage.Document.Attributes.GetValueOrDefault("layout") switch
			{
				"navigation" => "_SideNavigation",
				"navigation-scrollspy" => CmsPage.Document.Anchors != null ? "_SideNavigationScrollSpy" : "_SideNavigation",
				"scrollspy" => CmsPage.Document.Anchors != null ? "_ScrollSpy" : "_WideDefault",
				_ => "_WideDefault",
			};

			return layout;
		}


		public async Task<IActionResult> OnGet()
		{
			if (_content.RequestedDocument == null || !_content.RequestedDocument.ExactMatch)
				return NotFound();

			var authResult = await _content.Authorize(User);

			if (authResult != CmsContentService.AuthResult.Success)
				return new StatusCodeResult(403);

			CmsPage = new(_content, User.Identity?.IsAuthenticated ?? false);

			if (CmsPage.Document.Attributes.ContainsKey("no-cache"))
				this.HttpContext.Response.Headers.Append("Cache-Control", "max-age=0, no-store");

			ViewData["HomePage"] = CmsPage.Document.Breadcrumbs[0].Path;
			ViewData["Language"] = CmsPage.Language;
			ViewData["Title"] = CmsPage.Document.Title;
			ViewData["Theme"] = this.Request.Cookies["Theme"] ?? "light";

			return Page();
		}
	}
}
