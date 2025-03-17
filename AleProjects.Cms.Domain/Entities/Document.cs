using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using AleProjects.Cms.Domain.ValueObjects;


namespace AleProjects.Cms.Domain.Entities
{

	public class Document : ITreeNode<int>
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public int Parent { get; set; }

		public int Position { get; set; }
		
		[Required(AllowEmptyStrings = false), MaxLength(256)]
		public string Slug { get; set; }

		[Required(AllowEmptyStrings = false), MaxLength(256)]
		public string RootSlug { get; set; }

		[Required(AllowEmptyStrings = false)] 
		public string Path { get; set; }

		public long PathHash { get; set; }

		[Required(AllowEmptyStrings = false), MaxLength(256)]
		public string Title { get; set; }
		
		public string Summary { get; set; }

		public string CoverPicture { get; set; }

		[MaxLength(16)]
		public string Language { get; set; }
		
		public string Description { get; set; }
		
		[MaxLength(64)]
		public string Icon { get; set; }

		public string Tags { get; set; }

		public string AuthPolicies { get; set; }

		public bool Published { get; set; }
		
		public DateTimeOffset CreatedAt { get; set; }
		
		public DateTimeOffset ModifiedAt { get; set; }
		
		[MaxLength(64)] 
		public string EditorRoleRequired { get; set; }
		
		[Required(AllowEmptyStrings = false), MaxLength(260)]
		public string Author { get; set; }

		public List<DocumentPathNode> DocumentPathNodes { get; set; }

		public List<FragmentLink> FragmentLinks { get; set; }

		public List<Reference> References { get; set; }

		public List<DocumentAttribute> DocumentAttributes { get; set; }

		// ITreeNode<int> implementation

		[NotMapped]
		public string Caption => EditorRoleRequired;

		[NotMapped]
		public string Data { get; set; }

		[NotMapped]
		public bool Enabled => Published;
	}
}
