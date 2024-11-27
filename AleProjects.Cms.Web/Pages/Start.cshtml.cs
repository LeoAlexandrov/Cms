using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.RazorPages;

using AleProjects.Cms.Application.Dto;
using AleProjects.Cms.Application.Services;


namespace AleProjects.Cms.Web.Pages
{
	[IgnoreAntiforgeryToken(Order = 1001)]
	public class StartModel(UserManagementService ums, IHtmlLocalizer<SharedErrors> localizer) : PageModel
	{
		private static readonly object _lockObj = new();

		private readonly IHtmlLocalizer<SharedErrors> _errorsLocalizer = localizer;
		private readonly UserManagementService _ums = ums;

		public string ErrorMessage {get; private set;}

		[BindProperty]
		[Required]
		[EmailAddress]
		public string Account { get; set; }

		public void OnGet()
		{
			if (!_ums.NoUsers())
			{
				this.Response.Redirect("/auth");
				return;
			}
		}

		public void OnPost()
		{
			if (!ModelState.IsValid)
			{
				this.ErrorMessage = (string)_errorsLocalizer.GetString("Start_InvalidAccount");
				return;
			}

			lock (_lockObj)
			{
				if (!_ums.NoUsers())
				{
					this.ErrorMessage = (string)_errorsLocalizer.GetString("Start_LoginExists");
					return;
				}

				DtoCreateUser user = new()
				{
					Login = this.Account,
					Role = "Developer"
				};

				var _ = _ums.CreateUser(user, null).Result;
			}

			this.Response.Redirect("/auth");
		}
	}
}
