using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Asp.Versioning;


namespace AleProjects.Cms.Web.Api
{

	[Route("api/v{version:apiVersion}/[controller]/{action}")]
	[ApiVersion("1.0")]
	[ApiController]
	public class UIController(IHtmlLocalizer<SharedResources> sharedLocalizer) : ControllerBase
	{
		private readonly IHtmlLocalizer<SharedResources> _sharedLocalizer = sharedLocalizer;

		#region dto
		class UserProfile
		{
			public string Name { get; set; }
			public string Avatar { get; set; }
		}

		class NavigationMenuItem
		{
			public string Id { get; set; }
			public string Label { get; set; }
			public string Url { get; set; }
			public string Icon { get; set; }
		}

		class Status
		{
			public string Version { get; set; }
		}

		#endregion

		[HttpGet]
		[Authorize]
		public IActionResult NavigationMenu()
		{
			UserProfile user = new() 
			{ 
				Name = User.Identity.Name,
				Avatar = User.Claims.FirstOrDefault(c => c.Type == "avt")?.Value ?? "/images/default-user.webp"
			};

			NavigationMenuItem[] menu = [
				new() { Id = "documents", Label = _sharedLocalizer.GetString("Nav_Documents"), Url = "/documents", Icon = "text_snippet" },
				new() { Id = "media", Label = _sharedLocalizer.GetString("Nav_Media"), Url = "/media", Icon = "photo_library" },
				//new() { Id = "templates", Label = _sharedLocalizer.GetString("Nav_Templates"), Url = "/templates", Icon = "dynamic_form" },
				new() { Id = "schemata", Label = _sharedLocalizer.GetString("Nav_Schemata"), Url = "/schemata", Icon = "code" },
				new() { Id = "users", Label = _sharedLocalizer.GetString("Nav_Users"), Url = "/users", Icon = "people" },
				new() { Id = "events", Label = _sharedLocalizer.GetString("Nav_Events"), Url = "/events", Icon = "bolt" },
				//new() { Id = "webhooks", Label = _sharedLocalizer.GetString("Nav_Webhooks"), Url = "/webhooks", Icon = "webhook" },
				//new() { Id = "settings", Label = _sharedLocalizer.GetString("Nav_Settings"), Url = "/settings", Icon = "settings" }
			];

			Status status = new()
			{
				Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()
			};

			return Ok(new { user, menu, status });
		}
	}

}