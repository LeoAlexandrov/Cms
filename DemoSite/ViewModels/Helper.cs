using System;
using System.Collections.Generic;

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


		/// <summary>
		/// Creates pagination view-model for the document children.
		/// </summary>
		/// <param name="document">The document</param>
		/// <param name="visibleLinks">Links in the pagination</param>
		/// <param name="docPath">Path of the document</param>
		/// <returns>Pagination view-model structure</returns>
		public static Pagination CreatePagination(this Document document, int visibleLinks, string docPath)
		{
			if (visibleLinks < 1 || document.ChildrenTaken == 0 || document.TotalChildrenCount == 0)
				return default;

			if (visibleLinks < 3)
				visibleLinks = 3;

			int currentPage = document.ChildrenTakePosition / document.ChildrenTaken + 1;
			int totalPages = document.TotalChildrenCount / document.ChildrenTaken;

			if (document.TotalChildrenCount % document.ChildrenTaken != 0)
				totalPages++;

			List<PaginationLink> links;

			if (totalPages <= visibleLinks)
			{
				links = new(totalPages);

				for (int i = 1; i <= totalPages; i++)
					links.Add(new() { Label = i.ToString(), Link = i == currentPage ? null : (i == 1 ? docPath : $"{docPath}?p={i}") });
			}
			else if (currentPage < visibleLinks - 1)
			{
				links = new(visibleLinks + 1);

				for (int i = 1; i < visibleLinks; i++)
					links.Add(new() { Label = i.ToString(), Link = i == currentPage ? null : (i == 1 ? docPath : $"{docPath}?p={i}") });

				links.Add(new());
				links.Add(new() { Label = totalPages.ToString(), Link = $"{docPath}?p={totalPages}" });
			}
			else if (totalPages - currentPage < visibleLinks - 2)
			{
				links = new(visibleLinks + 1)
				{
					new() { Label = "1", Link = docPath },
					new()
				};

				for (int i = totalPages - visibleLinks + 2; i <= totalPages; i++)
					links.Add(new() { Label = i.ToString(), Link = i == currentPage ? null : $"{docPath}?p={i}" });
			}
			else
			{
				links = new(visibleLinks + 4)
				{
					new() { Label = "<", Link = $"{docPath}?p={currentPage - 1}" },
					new() { Label = "1", Link = docPath },
					new()
				};

				int from = currentPage - (visibleLinks - 2) / 2;
				int to = from + visibleLinks - 2;

				for (int i = from; i < to; i++)
					links.Add(new() { Label = i.ToString(), Link = i == currentPage ? null : $"{docPath}?p={i}" });

				links.Add(new());
				links.Add(new() { Label = totalPages.ToString(), Link = $"{docPath}?p={totalPages}" });
				links.Add(new() { Label = ">", Link = $"{docPath}?p={currentPage + 1}" });
			}


			return new Pagination() { Links = links };
		}
	}
}