using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AleProjects.Cms.Web.Pages
{
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	[IgnoreAntiforgeryToken]
	public class ErrorModel : PageModel
	{
		public int Code { get; set; }
		public string RequestId { get; set; }
		public bool ShowRequestId => Code == 500 && !string.IsNullOrEmpty(RequestId);


		public ErrorModel()
		{
		}

		public void OnGet(int? code)
		{
			Code = code ?? 500;
			RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
		}
	}

}
