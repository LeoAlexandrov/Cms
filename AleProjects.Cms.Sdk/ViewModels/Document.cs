using System;
using System.Collections.Generic;

using Entities = AleProjects.Cms.Domain.Entities;


namespace AleProjects.Cms.Sdk.ViewModels
{

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

		public string AssociatedClaims { get; set; }
		public bool Published { get; set; }
		public DateTimeOffset CreatedAt { get; set; }
		public DateTimeOffset ModifiedAt { get; set; }
		public string Author { get; set; }

		public BreadcrumbsItem[] Breadcrumbs { get; set; }
		public Document Parent { get; set; }
		public Document[] Siblings { get; set; }
		public Document[] Children { get; set; }

		public Fragment[] Fragments { get; set; }
		public Dictionary<string, Attribute> Attributes { get; set; }

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
				Tags = doc.Tags.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
				AssociatedClaims = doc.AssociatedClaims;
				Published = doc.Published;
				CreatedAt = doc.CreatedAt;
				ModifiedAt = doc.ModifiedAt;
				Author = doc.Author;
			}
		}
	}

}