using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;


namespace HCms.Web.Api
{

	[Route("api/v{version:apiVersion}/[controller]/{action=Get}")]
	[ApiVersion("1.0")]
	[ApiController]
	public class UIController(IAuthorizationService authService, IHtmlLocalizer<SharedResources> sharedLocalizer) : ControllerBase
	{
		const string PAGE_DOCUMENTS = "documents";
		const string PAGE_MEDIA = "media";
		const string PAGE_SCHEMATA = "schemata";
		const string PAGE_USERS = "users";
		const string PAGE_EVENTS = "events";

		private readonly IAuthorizationService _authService = authService;
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
		public async Task<IActionResult> Get(string id)
		{
			UserProfile user = new()
			{
				Name = User.Identity.Name,
				Avatar = User.Claims.FirstOrDefault(c => c.Type == "avt")?.Value ?? "/images/default-user.webp"
			};

			NavigationMenuItem[] menu = [
				new()
				{
					Id = PAGE_DOCUMENTS,
					Label = _sharedLocalizer.GetString("Nav_Documents"),
					Url = id == PAGE_DOCUMENTS ? null : "/documents",
					Icon = "text_snippet"
				},
				new() 
				{ 
					Id = PAGE_MEDIA, 
					Label = _sharedLocalizer.GetString("Nav_Media"), 
					Url = id == PAGE_MEDIA ? null : "/media", 
					Icon = "photo_library" 
				},
				new() 
				{ 
					Id = PAGE_SCHEMATA, 
					Label = _sharedLocalizer.GetString("Nav_Schemata"), 
					Url = id == PAGE_SCHEMATA ? null : "/schemata", 
					Icon = "code" 
				},
				new() 
				{ 
					Id = PAGE_USERS, 
					Label = _sharedLocalizer.GetString("Nav_Users"), 
					Url = id == PAGE_USERS ? null : "/users", 
					Icon = "people" 
				},
				new() 
				{ 
					Id = PAGE_EVENTS, 
					Label = _sharedLocalizer.GetString("Nav_Events"), 
					Url = id == PAGE_EVENTS ? null : "/events", 
					Icon = "bolt" 
				},
			];

			Status status = new()
			{
				Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()
			};

			var authResult = await _authService.AuthorizeAsync(User, "UploadUnsafeContent");
			
			Dictionary<string, object> parameters = id switch
			{
				PAGE_MEDIA => new Dictionary<string, object>
					{
						{ "uploadOnlySafeContent", !authResult.Succeeded }
					},

				_ => [],
			};

			return Ok(new { user, menu, status, parameters });
		}

	}
}