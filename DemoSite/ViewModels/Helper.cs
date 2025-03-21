using System;

using HCms.ViewModels;



namespace DemoSite.ViewModels
{
	/// <summary>
	/// Represents a helper class.
	/// </summary>
	public static class Helper
	{

		/// <summary>
		/// Returns the DOM ID for the outer tag of the fragment.
		/// </summary>
		/// <param name="fragment">The fragment.</param>
		/// <returns>The DOM ID.</returns>
		public static string DomId(this Fragment fragment)
		{
			if (fragment.Id == 0 || string.IsNullOrEmpty(fragment.Name))
				return null;

			int n = fragment.Name.Length;
			Span<char> cId = stackalloc char[n];

			for (int i = 0; i < n; i++)
				if (fragment.Name[i] == '-' || fragment.Name[i] == '_' || char.IsLetterOrDigit(fragment.Name[i]))
					cId[i] = fragment.Name[i];
				else
					cId[i] = '-';

			return new string(cId);
		}



		/// <summary>
		/// Returns the CSS class for the outer tag of the fragment.
		/// </summary>
		/// <param name="fragment">The fragment.</param>
		/// <returns>The CSS class.</returns>
		public static string CssClass(this Fragment fragment)
		{
			if (fragment.Container == 0)
				return $"{fragment.XmlName}-fragment";

			return $"{fragment.XmlName}-inner-fragment";
		}
	}
}