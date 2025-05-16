using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using AleProjects.Cms.Application.Services;


namespace AleProjects.Cms.Web.Pages
{

	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	[IgnoreAntiforgeryToken(Order = 1001)]
	public class StartModel(IHtmlLocalizer<SharedErrors> localizer) : PageModel
	{
		readonly static object _lockObj = new();
		readonly IHtmlLocalizer<SharedErrors> _errorsLocalizer = localizer;

		public string ErrorMessage {get; private set;}

		[BindProperty]
		[Required]
		public string Account { get; set; }

		[BindProperty]
		public bool AddDemoData { get; set; }


		public void OnGet([FromServices] UserManagementService ums)
		{
			if (!ums.NoUsers())
			{
				this.Response.Redirect("/auth");
				return;
			}
		}

		public void OnPost([FromServices] IServiceScopeFactory serviceScopeFactory, [FromServices] ILoggerFactory loggerFactory)
		{
			if (!ModelState.IsValid)
			{
				this.ErrorMessage = (string)_errorsLocalizer.GetString("Start_InvalidAccount");
				return;
			}

			lock (_lockObj)
			{
				var result = InitializationHelper.Initialize(serviceScopeFactory, this.Account, this.AddDemoData, loggerFactory).Result;

				if (result == InitializationHelper.InitResult.UsersExist)
				{
					this.ErrorMessage = (string)_errorsLocalizer.GetString("Start_LoginExists");
					return;
				}
			}

			this.Response.Redirect("/auth");
		}
	}
}
