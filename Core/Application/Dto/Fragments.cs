using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using AleProjects.Cms.Domain.Entities;
using AleProjects.Cms.Domain.ValueObjects;
using MessagePack;


namespace AleProjects.Cms.Application.Dto
{

	[MessagePackObject]
	public class DtoFragmentLiteResult
	{
		[MessagePack.Key("label")]
		public string Label { get; set; }

		[MessagePack.Key("value")]
		public string Value { get; set; }

		[MessagePack.Key("ns")]
		public string Ns { get; set; }
	}



	[MessagePackObject]
	public class DtoFragmentCreationStuffResult
	{
		[MessagePack.Key("shared")]
		public DtoFragmentLiteResult[] Shared { get; set; }

		[MessagePack.Key("templates")]
		public DtoFragmentLiteResult[] Templates { get; set; }
	}



	[MessagePackObject]
	public class DtoFragmentResult
	{
		[MessagePack.Key("id")]
		public int Id { get; set; }

		[MessagePack.Key("name")]
		public string Name { get; set; }

		[MessagePack.Key("icon")]
		[MaxLength(64)]
		public string Icon { get; set; }

		[MessagePack.Key("tags")]
		public string Tags { get; set; }

		[MessagePack.Key("shared")]
		public bool Shared { get; set; }

		[MessagePack.Key("xmlSchema")]
		public string XmlSchema { get; set; }

		[MessagePack.Key("xmlName")]
		public string XmlName { get; set; }


		public DtoFragmentResult() { }

		public DtoFragmentResult(Fragment fragment) 
		{
			if (fragment != null)
			{
				Id = fragment.Id;
				Name = fragment.Name;
				Icon = fragment.Icon;
				Tags = fragment.Tags;
				Shared = fragment.Shared;
				XmlSchema = fragment.XmlSchema;
				XmlName = fragment.XmlName;
			}
		}
	}



	[MessagePackObject]
	public class DtoFullFragment
	{
		[MessagePack.Key("properties")]
		public DtoFragmentResult Properties { get; set; }

		[MessagePack.Key("linkId")]
		[Required]
		public int LinkId { get; set; }

		[MessagePack.Key("enabled")]
		public bool Enabled { get; set; }

		[MessagePack.Key("decomposition")]
		public IReadOnlyList<DtoFragmentElement> Decomposition { get; set; }
	}



	[MessagePackObject]
	public class DtoFullFragmentResult : DtoFullFragment
	{
		[MessagePack.Key("lockShare")]
		public bool LockShare { get; set; }
	}



	[MessagePackObject]
	public class DtoFragmentLinkResult
	{
		[MessagePack.Key("id")]
		public int Id { get; set; }

		[MessagePack.Key("documentRef")]
		public int DocumentRef { get; set; }

		[MessagePack.Key("fragmentRef")]
		public int FragmentRef { get; set; }

		[MessagePack.Key("containerRef")]
		public int ContainerRef { get; set; }

		[MessagePack.Key("position")]
		public int Position { get; set; }

		[MessagePack.Key("enabled")]
		public bool Enabled { get; set; }

		[MessagePack.Key("data")]
		public string Data { get; set; }


		public DtoFragmentLinkResult() { }

		public DtoFragmentLinkResult(FragmentLink link)
		{
			if (link != null)
			{
				Id = link.Id;
				DocumentRef = link.DocumentRef;
				FragmentRef = link.FragmentRef;
				ContainerRef = link.ContainerRef;
				Position = link.Position;
				Enabled = link.Enabled;
				Data = link.Data;
			}
		}
	}



	[MessagePackObject]
	public class DtoFragmentElement(XSElement xse, string lang)
	{
		[MessagePack.Key("name")]
		public string Name { get; set; } = xse?.Name;

		[MessagePack.Key("namespace")]
		public string Namespace { get; set; } = xse?.Namespace;

		[MessagePack.Key("xmlType")]
		public string XmlType { get; set; } = xse?.XmlType;

		[MessagePack.Key("isSimple")]
		public bool IsSimple { get; set; } = xse == null || xse.IsSimple;

		[MessagePack.Key("defaultValue")]
		public object DefaultValue { get; set; } = xse?.DefaultObjectValue();

		[MessagePack.Key("value")]
		public object Value { get; set; }

		[MessagePack.Key("annotation")]
		public string Annotation { get; set; } = xse?.GetAnnotationDoc(lang);

		[MessagePack.Key("path")]
		public string Path { get; set; }

		[MessagePack.Key("level")]
		public int Level { get; set; }

		[MessagePack.Key("facetEnumeration")]
		public IList<string> FacetEnumeration { get; set; } = xse?.FacetEnumeration;

		[MessagePack.Key("facetPattern")]
		public string FacetPattern { get; set; } = xse?.FacetPattern;

		[MessagePack.Key("facetMinInclusive")]
		public int? FacetMinInclusive { get; set; } = xse?.FacetMinInclusive;

		[MessagePack.Key("facetMaxInclusive")]
		public int? FacetMaxInclusive { get; set; } = xse?.FacetMaxInclusive;

		[MessagePack.Key("facetMinExclusive")]
		public int? FacetMinExclusive { get; set; } = xse?.FacetMinExclusive;

		[MessagePack.Key("facetMaxExclusive")]
		public int? FacetMaxExclusive { get; set; } = xse?.FacetMaxExclusive;

		[MessagePack.Key("facetMinLength")]
		public int? FacetMinLength { get; set; } = xse?.FacetMinLength;

		[MessagePack.Key("facetMaxLength")]
		public int? FacetMaxLength { get; set; } = xse?.FacetMaxLength;

		[MessagePack.Key("minOccurs")]
		public int MinOccurs { get; set; } = xse == null ? 1 : xse.MinOccurs;

		[MessagePack.Key("maxOccurs")]
		public int MaxOccurs { get; set; } = xse == null ? 1 : xse.MaxOccurs;

		[MessagePack.Key("isContainer")]
		public bool IsContainer { get; set; } = xse != null && xse.RepresentsContainer;

		[MessagePack.Key("isImage")]
		public bool IsImage { get; set; } = xse != null && xse.RepresentsImage;

		[MessagePack.Key("textFormat")]
		public string TextFormat { get; set; } = xse?.InnerTextFormat;

		[MessagePack.Key("isAddable")]
		public bool IsAddable { get; set; }

		public DtoFragmentElement() : this(null, null) { }
	}



	[MessagePackObject]
	public class DtoCreateFragment
	{
		[MessagePack.Key("document")]
		[Required]
		public int Document { get; set; }

		[MessagePack.Key("parent")]
		[Required]
		public int Parent { get; set; }

		[MessagePack.Key("name")]
		[Required(AllowEmptyStrings = false), MaxLength(256)]
		public string Name { get; set; }

		[MessagePack.Key("templateName")]
		public string TemplateName { get; set; }

		[MessagePack.Key("sharedFragment")]
		public string SharedFragment { get; set; }

		[MessagePack.Key("schema")]
		[/*Required(AllowEmptyStrings = false),*/ MaxLength(256)]
		public string Schema { get; set; }
	}



	[MessagePackObject]
	public class DtoFragmentChangeResult : DtoDocumentChangeResult
	{
		[MessagePack.Key("fragment")]
		public DtoFragmentResult Fragment { get; set; }

		[MessagePack.Key("sharedStateChanged")]
		public bool SharedStateChanged { get; set; }

		[MessagePack.Key("link")]
		public DtoFragmentLinkResult Link { get; set; }
	}



	[MessagePackObject]
	public class DtoMoveFragment
	{
		[MessagePack.Key("increment")]
		[Required]
		public int Increment { get; set; }
	}



	[MessagePackObject]
	public class DtoMoveFragmentResult : DtoDocumentChangeResult
	{
		[MessagePack.Key("newPosition")]
		public int NewPosition { get; set; }

		[MessagePack.Key("oldPosition")]
		public int OldPosition { get; set; }
	}

}