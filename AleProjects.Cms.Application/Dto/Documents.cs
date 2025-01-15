using System;
using System.ComponentModel.DataAnnotations;

using AleProjects.Cms.Domain.Entities;
using MessagePack;


namespace AleProjects.Cms.Application.Dto
{

	[MessagePackObject]
	public class DtoDocumentResult
	{
		[MessagePack.Key("id")]
		public int Id { get; set; }

		[MessagePack.Key("parent")]
		public int Parent { get; set; }

		[MessagePack.Key("position")]
		public int Position { get; set; }

		[MessagePack.Key("slug")]
		public string Slug { get; set; }

		[MessagePack.Key("path")]
		public string Path { get; set; }

		[MessagePack.Key("title")]
		public string Title { get; set; }

		[MessagePack.Key("summary")]
		public string Summary { get; set; }

		[MessagePack.Key("coverPicture")]
		public string CoverPicture { get; set; }

		[MessagePack.Key("language")]
		public string Language { get; set; }

		[MessagePack.Key("description")]
		public string Description { get; set; }

		[MessagePack.Key("icon")]
		public string Icon { get; set; }

		[MessagePack.Key("tags")]
		public string Tags { get; set; }

		[MessagePack.Key("associatedClaims")]
		public string AssociatedClaims { get; set; }

		[MessagePack.Key("published")]
		public bool Published { get; set; }

		[MessagePack.Key("createdAt")]
		public DateTimeOffset CreatedAt { get; set; }

		[MessagePack.Key("modifiedAt")]
		public DateTimeOffset ModifiedAt { get; set; }

		[MessagePack.Key("editorRoleRequired")]
		public string EditorRoleRequired { get; set; }

		[MessagePack.Key("author")]
		public string Author { get; set; }

		public DtoDocumentResult() { }

		public DtoDocumentResult(Document doc)
		{
			if (doc != null)
			{
				Id = doc.Id;
				Parent = doc.Parent;
				Position = doc.Position;
				Slug = doc.Slug;
				Path = doc.Path;
				Title = doc.Title;
				Summary = doc.Summary;
				CoverPicture = doc.CoverPicture;
				Language = doc.Language;
				Description = doc.Description;
				Icon = doc.Icon;
				Tags = doc.Tags;
				AssociatedClaims = doc.AssociatedClaims;
				Published = doc.Published;
				CreatedAt = doc.CreatedAt;
				ModifiedAt = doc.ModifiedAt;
				EditorRoleRequired = doc.EditorRoleRequired;
				Author = doc.Author;
			}
		}
	}



	[MessagePackObject]
	public class DtoFullDocumentResult
	{
		[MessagePack.Key("properties")]
		public DtoDocumentResult Properties { get; set; }

		[MessagePack.Key("fragmentLinks")]
		public DtoFragmentLinkResult[] FragmentLinks { get; set; }

		[MessagePack.Key("attributes")]
		public DtoDocumentAttributeResult[] Attributes { get; set; }

		[MessagePack.Key("fragmentsTree")]
		public DtoTreeNode<int>[] FragmentsTree { get; set; }
	}



	[MessagePackObject]
	public class DtoDocumentChangeResult
	{
		[MessagePack.Key("author")]
		public string Author { get; set; }

		[MessagePack.Key("modifiedAt")]
		public DateTimeOffset ModifiedAt { get; set; }
	}



	[MessagePackObject(AllowPrivate = true)]
	public class DtoCreateDocument
	{
		[MessagePack.IgnoreMember]
		protected string slug;

		[MessagePack.Key("parent")]
		[Required]
		public int Parent { get; set; }

		[MessagePack.Key("slug")]
		[Required(AllowEmptyStrings = false), MaxLength(256)]
		public string Slug { get => slug; set => slug = value?.ToLower(); }

		[MessagePack.Key("title")]
		[Required(AllowEmptyStrings = false), MaxLength(256)]
		public string Title { get; set; }

		[MessagePack.Key("inheritAttributes")]
		public bool InheritAttributes { get; set; }
	}



	[MessagePackObject(AllowPrivate = true)]
	public class DtoUpdateDocument
	{
		[MessagePack.IgnoreMember]
		protected string slug;

		[MessagePack.Key("slug")]
		[Required(AllowEmptyStrings = false), MaxLength(256)]
		public string Slug { get => slug; set => slug = value?.ToLower(); }

		[MessagePack.Key("title")]
		[Required(AllowEmptyStrings = false), MaxLength(256)]
		public string Title { get; set; }

		[MessagePack.Key("summary")]
		public string Summary { get; set; }

		[MessagePack.Key("coverPicture")]
		public string CoverPicture { get; set; }

		[MessagePack.Key("language")]
		[RegularExpression(@"^\w\w(\-\w\w)?$")]
		public string Language { get; set; }

		[MessagePack.Key("description")]
		public string Description { get; set; }

		[MessagePack.Key("icon")]
		[MaxLength(64)]
		public string Icon { get; set; }

		[MessagePack.Key("tags")]
		public string Tags { get; set; }

		[MessagePack.Key("associatedClaims")]
		public string AssociatedClaims { get; set; }

		[MessagePack.Key("published")]
		[Required]
		public bool Published { get; set; }
	}



	[MessagePackObject]
	public class DtoLockDocument
	{
		[MessagePack.Key("lockState")]
		[Required]
		public bool LockState { get; set; }
	}



	[MessagePackObject]
	public class DtoSetParentDocument
	{
		[MessagePack.Key("parent")]
		[Required]
		public int Parent { get; set; }
	}



	[MessagePackObject]
	public class DtoMoveDocument
	{
		[MessagePack.Key("increment")]
		[Required]
		public int Increment { get; set; }
	}



	[MessagePackObject]
	public class DtoMoveDocumentResult: DtoDocumentChangeResult
	{
		[MessagePack.Key("newPosition")]
		public int NewPosition { get; set; }

		[MessagePack.Key("oldPosition")]
		public int OldPosition { get; set; }
	}



	[MessagePackObject]
	public class DtoCopyDocument
	{
		[MessagePack.Key("origin")]
		[Required]
		public int Origin { get; set; }
	}

}