using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace HCms.Web.Pages
{

	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public class SettingsModel : PageModel
	{
		public void OnGet()
		{
		}
	}
}
