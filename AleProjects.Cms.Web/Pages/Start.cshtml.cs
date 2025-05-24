using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
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
		readonly static SemaphoreSlim _semaphore = new(1, 1);
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

		public async Task<IActionResult> OnPost([FromServices] IServiceScopeFactory serviceScopeFactory, [FromServices] ILoggerFactory loggerFactory)
		{
			if (!ModelState.IsValid)
			{
				this.ErrorMessage = (string)_errorsLocalizer.GetString("Start_InvalidAccount");
				return Page();
			}

			await _semaphore.WaitAsync();

			try
			{
				var result = await InitializationHelper.Initialize(serviceScopeFactory, this.Account, this.AddDemoData, loggerFactory);

				if (result == InitializationHelper.InitResult.UsersExist)
				{
					this.ErrorMessage = (string)_errorsLocalizer.GetString("Start_LoginExists");
					return Page();
				}
			}
			finally
			{
				_semaphore.Release();
			}

			return Redirect("/auth");
		}
	}
}
