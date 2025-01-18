using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;



namespace AleProjects.Cms.Domain.ValueObjects
{

	public class XSElement
	{
		public string Name { get; set; }
		public string Namespace { get; set; }
		public string XmlType { get; set; }
		public bool IsSimple { get; set; }
		public object DefaultValue { get; set; }

		//[JsonIgnore]
		[XmlIgnore]
		public IList<XSElement> Elements { get; set; }

		public IList<string> FacetEnumeration { get; set; }
		public string FacetPattern { get; set; }
		public int? FacetMinInclusive { get; set; }
		public int? FacetMaxInclusive { get; set; }
		public int? FacetMinExclusive { get; set; }
		public int? FacetMaxExclusive { get; set; }
		public int? FacetMinLength { get; set; }
		public int? FacetMaxLength { get; set; }
		public int MinOccurs { get; set; } = 1;
		public int MaxOccurs { get; set; } = 1;

		[JsonIgnore]
		[XmlIgnore]
		public XSElement Parent { get; set; }

		[JsonIgnore]
		[XmlIgnore]
		public XSElement Root
		{
			get
			{
				var result = this;

				while (result.Parent != null)
					result = result.Parent;

				return result;
			}
		}



		//[JsonIgnore]
		[XmlIgnore]
		public string Path
		{
			get
			{
				List<string> path = new(16) { Namespace + ":" + Name };
				XSElement parent = this.Parent;

				while (parent != null)
				{
					path.Add(parent.Namespace + ":" + parent.Name);
					parent = parent.Parent;
				}

				return string.Join('\\', path.AsEnumerable().Reverse());
			}
		}

		//[JsonIgnore]
		[XmlIgnore]
		public XSAnnotation? Annotation { get; set; }

		//[JsonIgnore]
		[XmlIgnore]
		public bool RepresentsContainer => this.Parent == null && this.Annotation.HasValue && this.Annotation.Value.IsContainer;

		//[JsonIgnore]
		[XmlIgnore]
		public bool RepresentsImage => this.Annotation.HasValue && this.Annotation.Value.IsImage;

		//[JsonIgnore]
		[XmlIgnore]
		public string InnerTextFormat => this.Annotation.GetValueOrDefault().TextFormat;


		public struct XSAnnotation
		{
			public Dictionary<string, string> Documentation { get; set; }
			public bool IsContainer { get; set; }
			public bool IsImage { get; set; }
			public string TextFormat { get; set; }
		}

		public List<XSElement> ElementsLinearly(List<XSElement> list)
		{
			if (list.Count != 0)
				list.Add(this);

			if (Elements != null)
				for (int i = 0; i < Elements.Count; i++)
					Elements[i].ElementsLinearly(list);

			return list;
		}

		public int DefaultIntValue()
		{
			int min, max;

			if (FacetMinInclusive.HasValue)
				min = FacetMinInclusive.Value;
			else if (FacetMinExclusive.HasValue)
				min = FacetMinExclusive.Value + 1;
			else
				min = int.MinValue;

			if (FacetMaxInclusive.HasValue)
				max = FacetMaxInclusive.Value;
			else if (FacetMaxExclusive.HasValue)
				max = FacetMaxExclusive.Value - 1;
			else
				max = int.MaxValue;

			if (min <= 0 && max >= 0)
				return 0;

			return min;
		}

		public double DefaultDoubleValue()
		{
			double min, max;

			if (FacetMinInclusive.HasValue)
				min = FacetMinInclusive.Value;
			else if (FacetMinExclusive.HasValue)
				min = FacetMinExclusive.Value + 1;
			else
				min = double.MinValue;

			if (FacetMaxInclusive.HasValue)
				max = FacetMaxInclusive.Value;
			else if (FacetMaxExclusive.HasValue)
				max = FacetMaxExclusive.Value - 1;
			else
				max = double.MaxValue;

			if (min <= 0.0 && max >= 0.0)
				return 0.0;

			return min;
		}

		public string GetAnnotationDoc(string lang)
		{
			var docs= Annotation.GetValueOrDefault().Documentation;

			if (docs == null || (!docs.TryGetValue(lang, out string annotationDoc) && !docs.TryGetValue("en", out annotationDoc)))
				annotationDoc = Name;

			return annotationDoc;
		}

		public object DefaultObjectValue()
		{
			if (this.DefaultValue != null)
				return this.DefaultValue;

			return this.XmlType switch
			{
				"token" => FacetEnumeration != null && FacetEnumeration.Count > 0 ? FacetEnumeration[0] : string.Empty,
				"boolean" => false,
				"integer" or "int" or "short" or "byte" => this.DefaultIntValue(),
				"decimal" or "double" or "float" => this.DefaultDoubleValue(),
				_ => string.Empty
			};
		}

		public string DefaultXmlValue()
		{
			if (this.DefaultValue != null)
			{
				if (this.XmlType == "boolean")
					return this.DefaultValue.ToString().ToLower();

				return this.DefaultValue.ToString();
			}

			return this.XmlType switch
			{
				"token" => FacetEnumeration != null && FacetEnumeration.Count > 0 ? FacetEnumeration[0] : string.Empty,
				"boolean" => "false",
				"integer" or "int" or "short" or "byte" => this.DefaultIntValue().ToString(),
				"decimal" or "double" or "float" => this.DefaultDoubleValue().ToString(new NumberFormatInfo()),
				_ => string.Empty
			};
		}

		public void AddSelfToIndex(Dictionary<string, XSElement> index)
		{
			index.Add(this.Path, this);

			if (Elements != null)
				for (int i = 0; i < Elements.Count; i++)
					Elements[i].AddSelfToIndex(index);
		}

		public string AddSelfNamespaces(Dictionary<string, string> nss, string prefix)
		{
			if (nss.TryAdd(this.Namespace, prefix))
				prefix = NextNSPrefix(prefix);

			if (Elements != null)
				for (int i = 0; i < Elements.Count; i++)
					prefix = Elements[i].AddSelfNamespaces(nss, prefix);

			return prefix;
		}

		public static string NextNSPrefix(string prefix)
		{
			if (string.IsNullOrEmpty(prefix))
				return "t";

			if (prefix.Length == 1)
				return "t1";

			return "t" + (int.Parse(prefix[1..]) + 1).ToString();
		}

	}


}