﻿using System;
using System.ComponentModel.DataAnnotations;

using MessagePack;

using AleProjects.Cms.Domain.Entities;


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

		[MessagePack.Key("authPolicies")]
		public string AuthPolicies { get; set; }

		[MessagePack.Key("publishStatus")]
		public int PublishStatus { get; set; }

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
				Path = string.IsNullOrEmpty(doc.Path) ? "/" : doc.Path;
				Title = doc.Title;
				Summary = doc.Summary;
				CoverPicture = doc.CoverPicture;
				Language = doc.Language;
				Description = doc.Description;
				Icon = doc.Icon;
				Tags = doc.Tags;
				AuthPolicies = doc.AuthPolicies;
				PublishStatus = doc.PublishStatus;
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
	public class DtoMinDocumentResult
	{
		[MessagePack.Key("id")]
		public int Id { get; set; }

		[MessagePack.Key("title")]
		public string Title { get; set; }
	}



	[MessagePackObject]
	public class DtoDocumentFragmentsResult
	{
		[MessagePack.Key("fragmentLinks")]
		public DtoFragmentLinkResult[] FragmentLinks { get; set; }

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
		[RequiredNonNegative]
		public int Parent { get; set; }

		[MessagePack.Key("slug")]
		[Required(AllowEmptyStrings = false), MaxLength(256)]
		public string Slug { get => slug; set => slug = value?.ToLower(); }

		[MessagePack.Key("title")]
		[Required(AllowEmptyStrings = false), MaxLength(256)]
		public string Title { get; set; }

		[MessagePack.Key("publishStatus")]
		public int PublishStatus { get; set; }


		[MessagePack.Key("copyAttributes")]
		public bool CopyAttributes { get; set; }
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

		[MessagePack.Key("authPolicies")]
		public string AuthPolicies { get; set; }

		[MessagePack.Key("publishStatus")]
		[Required]
		public int? PublishStatus { get; set; }
	}



	[MessagePackObject]
	public class DtoLockDocument
	{
		[MessagePack.Key("lockState")]
		[Required]
		public bool? LockState { get; set; }
	}



	[MessagePackObject]
	public class DtoSetParentDocument
	{
		[MessagePack.Key("parent")]
		[RequiredNonNegative]
		public int Parent { get; set; }
	}



	[MessagePackObject]
	public class DtoMoveDocument
	{
		[MessagePack.Key("increment")]
		[Required]
		public int? Increment { get; set; }
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
		[RequiredPositive]
		public int Origin { get; set; }
	}



	[MessagePackObject]
	public class DtoDocumentRefResult
	{
		[MessagePack.Key("references")]
		public DtoMinDocumentResult[] References { get; set; }

		[MessagePack.Key("referencedBy")]
		public DtoMinDocumentResult[] ReferencedBy { get; set; }
	}

}