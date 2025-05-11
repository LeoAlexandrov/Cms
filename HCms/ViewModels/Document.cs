using System;
using System.Collections.Generic;

using Entities = AleProjects.Cms.Domain.Entities;


namespace HCms.ViewModels
{
	/// <summary>
	/// Represents a document view model.
	/// </summary>
	public class Document
	{
		public int Id { get; set; }
		public int ParentId { get; set; }
		public int Position { get; set; }
		public string Slug { get; set; }
		public string Path { get; set; }
		public string Title { get; set; }
		public string Summary { get; set; }
		public string CoverPicture { get; set; }
		public string Language { get; set; }
		public string Icon { get; set; }
		public string[] Tags { get; set; }

		public string AuthPolicies { get; set; }
		public int PublishStatus { get; set; }
		public DateTimeOffset CreatedAt { get; set; }
		public DateTimeOffset ModifiedAt { get; set; }
		public string Author { get; set; }

		public bool ExactMatch { get; set; } = true;
		public BreadcrumbsItem[] Breadcrumbs { get; set; }
		public Document Parent { get; set; }
		public Document Root { get; set; }
		public Document[] Siblings { get; set; }
		public Document[] Children { get; set; }

		public int ChildrenTakePosition { get; set; } = -1;
		public int ChildrenTaken { get; set; }
		public int TotalChildrenCount { get; set; }

		public Fragment[] Fragments { get; set; }
		public Dictionary<string, string> Attributes { get; set; }
		public List<Anchor> Anchors { get; set; }

		public bool AuthRequired { get => !(string.IsNullOrEmpty(AuthPolicies) || AuthPolicies.StartsWith("//")); }

		public Document() { }

		public Document(Entities.Document doc)
		{
			if (doc != null)
			{
				Id = doc.Id;
				ParentId = doc.Parent;
				Position = doc.Position;
				Slug = doc.Slug;
				Path = doc.Path;
				Title = doc.Title;
				Summary = doc.Summary;
				CoverPicture = doc.CoverPicture;
				Language = doc.Language;
				Icon = doc.Icon;
				Tags = doc.Tags?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
				AuthPolicies = doc.AuthPolicies;
				PublishStatus = doc.PublishStatus;
				CreatedAt = doc.CreatedAt;
				ModifiedAt = doc.ModifiedAt;
				Author = doc.Author;
			}
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