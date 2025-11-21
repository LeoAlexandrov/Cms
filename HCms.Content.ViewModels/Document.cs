using System;
using System.Collections.Generic;

using MessagePack;


namespace HCms.Content.ViewModels
{
	/// <summary>
	/// Represents a document view model. For use as razor page or partial view model.
	/// </summary>
	[MessagePackObject]
	public class Document
	{
		[MessagePack.Key("id")]
		public int Id { get; set; }

		[MessagePack.Key("parentId")]
		public int ParentId { get; set; }

		[MessagePack.Key("position")]
		public int Position { get; set; }

		[MessagePack.Key("slug")]
		public string Slug { get; set; }

		[MessagePack.Key("path")]
		public string Path { get; set; }

		[MessagePack.Key("root")]
		public string Root { get; set; }

		[MessagePack.Key("url")]
		public string Url { get; set; }

		[MessagePack.Key("title")]
		public string Title { get; set; }

		[MessagePack.Key("summary")]
		public string Summary { get; set; }

		[MessagePack.Key("coverPicture")]
		public string CoverPicture { get; set; }

		[MessagePack.Key("language")]
		public string Language { get; set; }

		[MessagePack.Key("icon")]
		public string Icon { get; set; }

		[MessagePack.Key("tags")]
		public string[] Tags { get; set; }

		[MessagePack.Key("authPolicies")]
		public string AuthPolicies { get; set; }

		[MessagePack.Key("status")]
		public int Status { get; set; }

		[MessagePack.Key("createdAt")]
		public DateTime CreatedAt { get; set; }

		[MessagePack.Key("modifiedAt")]

		public DateTime ModifiedAt { get; set; }

		[MessagePack.Key("author")]
		public string Author { get; set; }

		[MessagePack.Key("exactMatch")]
		public bool ExactMatch { get; set; } = true;

		[MessagePack.Key("breadcrumbs")]
		public BreadcrumbsItem[] Breadcrumbs { get; set; }

		[MessagePack.Key("siblings")]
		public Document[] Siblings { get; set; }

		[MessagePack.Key("children")]
		public Document[] Children { get; set; }

		[MessagePack.Key("childrenTakePosition")]
		public int ChildrenTakePosition { get; set; } = -1;

		[MessagePack.Key("childrenTaken")]
		public int ChildrenTaken { get; set; }

		[MessagePack.Key("totalChildrenCount")]
		public int TotalChildrenCount { get; set; }

		[MessagePack.Key("fragments")]
		public Fragment[] Fragments { get; set; }

		[MessagePack.Key("attributes")]
		public Dictionary<string, string> Attributes { get; set; }

		[MessagePack.Key("anchors")]
		public List<Anchor> Anchors { get; set; }

		[MessagePack.Key("authRequired")]
		public bool AuthRequired { get => !(string.IsNullOrEmpty(AuthPolicies) || AuthPolicies.StartsWith("//")); }


		public Document() { }


		public List<T> BasicPagination<T>(int visibleLinks)
			where T : ILink, new()
		{
			if (visibleLinks < 1 || ChildrenTaken == 0 || TotalChildrenCount == 0)
				return [];

			string docPath = Url;

			if (visibleLinks < 3)
				visibleLinks = 3;

			int currentPage = ChildrenTakePosition / ChildrenTaken + 1;
			int totalPages = TotalChildrenCount / ChildrenTaken;

			if (TotalChildrenCount % ChildrenTaken != 0)
				totalPages++;

			List<T> links;

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

			return links;
		}

		public Fragment FindFragment(int id)
		{
			if (id <= 0 || Fragments == null)
				return null;

			static Fragment find(Fragment fragment, int id)
			{
				if (fragment.Id == id)
					return fragment;

				Fragment result;

				if (fragment.Children != null)
					for (int i = 0; i < fragment.Children.Length; i++)
						if ((result = find(fragment.Children[i], id)) != null)
							return result;

				return null;
			}

			Fragment result = null;

			for (int i = 0; i < Fragments.Length; i++)
				if ((result = find(Fragments[i], id)) != null)
					break;

			return result;
		}
	}

}