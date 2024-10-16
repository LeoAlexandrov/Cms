using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using Microsoft.EntityFrameworkCore;

using AleProjects.Cms.Domain.Entities;
using AleProjects.Cms.Domain.ValueObjects;
using AleProjects.Cms.Infrastructure.Data;
using System.Text.Json;



namespace AleProjects.Cms.Application.Services
{

	public class FragmentSchemaService
	{
		public const string W3_2001_XMLSCHEMA = "http://www.w3.org/2001/XMLSchema";

		public List<XSElement> Fragments { get; private set; }
		public XmlSchemaSet SchemaSet { get; private set; }
		public Dictionary<string, XSElement> Index { get; private set; }


		public FragmentSchemaService() { }

		public FragmentSchemaService(CmsDbContext dbContext)
		{
			ArgumentNullException.ThrowIfNull(dbContext);

			(SchemaSet, Fragments) = ReadSchemata(dbContext);
			Index = [];

			foreach (var f in Fragments)
				f.AddSelfToIndex(Index);
		}

		private static XmlSchema ReadXsd(string data)
		{
			using var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
			using var reader = XmlReader.Create(stream);

			XmlSchema schema = XmlSchema.Read(reader, null);

			return schema;
		}

		private static XSElement.XSAnnotation? GetAnnotation(XmlSchemaAnnotated annotated)
		{
			if (annotated?.Annotation == null)
				return null;

			XSElement.XSAnnotation result = new();
			StringBuilder sb = null;

			foreach (var item in annotated.Annotation.Items)
				if (item is XmlSchemaDocumentation doc)
				{

					result.Documentation ??= [];

					if (sb == null)
						sb = new(256);
					else
						sb.Clear();

					if (doc.Markup != null)
					{
						int n = doc.Markup.Length - 1;

						for (int i = 0; i <= n; i++)
							if (!string.IsNullOrEmpty(doc.Markup[i].Value))
								if (i < n)
									sb.AppendLine(doc.Markup[i].Value.Trim());
								else
									sb.Append(doc.Markup[i].Value.Trim());
					}

					result.Documentation[doc.Language ?? "invariant"] = sb.ToString();
				}
				else if (item is XmlSchemaAppInfo appInfo)
				{
					if (appInfo.Markup != null)
					{
						for (int i = 0; i < appInfo.Markup.Length; i++)
							if (appInfo.Markup[i] != null && appInfo.Markup[i].LocalName == "properties")
								foreach (XmlAttribute attr in appInfo.Markup[i].Attributes)
									switch (attr.Name)
									{
										case "container":
											result.IsContainer = string.Compare(attr.Value, "true", StringComparison.CurrentCultureIgnoreCase) == 0;
											break;

										case "image":
											result.IsImage = string.Compare(attr.Value, "true", StringComparison.CurrentCultureIgnoreCase) == 0;
											break;

										case "textformat":
											result.TextFormat = attr.Value.ToLower(); 
											break;
									}


					}


				}


			return result;
		}

		private static XSElement TraverseSchema(XmlSchemaElement element, XSElement parent)
		{
			if (element == null)
				return null;

			//Console.WriteLine(element.Name + " = " + element.QualifiedName.Namespace + " / " + element.SchemaTypeName.Namespace);

			XSElement e = new()
			{
				Name = element.Name,
				Namespace = element.QualifiedName.Namespace,
				Parent = parent,
				MinOccurs = (int)element.MinOccurs,
				MaxOccurs = element.MaxOccursString == "unbounded" ? int.MaxValue : (int)element.MaxOccurs
			};


			switch (element.ElementSchemaType)
			{

				case XmlSchemaComplexType complexType:

					e.XmlType = element.SchemaTypeName.Name;
					e.Annotation = GetAnnotation(complexType);

					/*
					if (complexType.AttributeUses.Count > 0)
					{
						var enumerator = complexType.AttributeUses.GetEnumerator();

						while (enumerator.MoveNext())
						{
							XmlSchemaAttribute attribute = (XmlSchemaAttribute)enumerator.Value;
							Console.WriteLine(attribute.FixedValue);
						}
					}
					*/

					if (complexType.ContentTypeParticle is XmlSchemaSequence sequence)
					{
						//e.MinOccurs = (int)sequence.MinOccurs;
						//e.MaxOccurs = sequence.MaxOccursString == "unbounded" ? int.MaxValue : (int)sequence.MaxOccurs;

						foreach (var childElement in sequence.Items)
							if (childElement is XmlSchemaElement elem)
							{
								var e1 = TraverseSchema(elem, e);

								if (e1 != null)
								{
									e.Elements ??= [];
									e1.Annotation ??= GetAnnotation(elem);
									e.Elements.Add(e1);
								}
							}
							//else if (childElement is XmlSchemaAny any)
							//{
							//	e.Annotation ??= GetAnnotation(any);
							//}
					}

					break;


				case XmlSchemaSimpleType simpleType:

					e.IsSimple = true;
					e.Annotation = GetAnnotation(element) ?? GetAnnotation(simpleType);

					if (simpleType.Content is XmlSchemaSimpleTypeRestriction restriction)
					{
						e.XmlType = restriction.Facets.Count != 0 ?
							restriction.BaseTypeName.Name :
							element.SchemaTypeName.Name;


						foreach (var facet in restriction.Facets)
						{
							switch (facet)
							{
								case XmlSchemaEnumerationFacet facetEnum:
									e.FacetEnumeration ??= [];
									e.FacetEnumeration.Add(facetEnum.Value);
									break;

								case XmlSchemaMinInclusiveFacet facetMinInclusiveFacet:
									if (int.TryParse(facetMinInclusiveFacet.Value, out int minInclusive))
										e.FacetMinInclusive = minInclusive;
									break;

								case XmlSchemaMaxInclusiveFacet facetMaxInclusiveFacet:
									if (int.TryParse(facetMaxInclusiveFacet.Value, out int maxInclusive))
										e.FacetMaxInclusive = maxInclusive;
									break;

								case XmlSchemaMinExclusiveFacet facetMinExclusiveFacet:
									if (int.TryParse(facetMinExclusiveFacet.Value, out int minExclusive))
										e.FacetMinExclusive = minExclusive;
									break;

								case XmlSchemaMaxExclusiveFacet facetMaxExclusiveFacet:
									if (int.TryParse(facetMaxExclusiveFacet.Value, out int maxExclusive))
										e.FacetMinExclusive = maxExclusive;
									break;

								case XmlSchemaPatternFacet facetPattern:
									e.FacetPattern = facetPattern.Value;
									break;

								case XmlSchemaMinLengthFacet facetMinLength:
									if (int.TryParse(facetMinLength.Value, out int minLength))
										e.FacetMinLength = minLength;
									break;

								case XmlSchemaMaxLengthFacet facetMaxLength:
									if (int.TryParse(facetMaxLength.Value, out int maxLength))
										e.FacetMaxLength = maxLength;
									break;
							}
						}
					}

					if (!string.IsNullOrEmpty(element.DefaultValue))
					{
						switch (e.XmlType)
						{
							case "string":
							case "normalizedString":

								e.DefaultValue = element.DefaultValue;
								break;

							case "integer":
							case "int":
							case "short":
							case "byte":

								if (int.TryParse(element.DefaultValue, out int iDef))
									e.DefaultValue = iDef;

								break;

							case "decimal":
							case "double":
							case "float":

								if (double.TryParse(element.DefaultValue, out double fDef))
									e.DefaultValue = fDef;

								break;

							case "boolean":

								if (string.Compare(element.DefaultValue, "true", StringComparison.InvariantCultureIgnoreCase) == 0)
									e.DefaultValue = "true";
								else if (string.Compare(element.DefaultValue, "false", StringComparison.InvariantCultureIgnoreCase) == 0)
									e.DefaultValue = "false";
								break;
						}
					}

					break;
			}

			return e;
		}

		private static List<Schema> CreateDefault(CmsDbContext dbContext)
		{
			List<Schema> result = [];

			if (dbContext.Schemata.Any())
				return result;

#if DEBUG
			string[] files = Directory.GetFiles("../SharedCode/XmlSchemas", "*.xsd", SearchOption.TopDirectoryOnly);
#else
			string[] files = Directory.GetFiles("XmlSchemas", "*.xsd", SearchOption.TopDirectoryOnly);
#endif

			foreach (string file in files)
			{
				try
				{
					var xml = File.ReadAllText(file);
					var schema = ReadXsd(xml);
					var ns = schema.TargetNamespace;

					var sch = new Schema() { Data = xml, Description = Path.GetFileName(file) + " (default)", Namespace = ns };

					dbContext.Schemata.Add(sch);
					result.Add(sch);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
			}

			dbContext.SaveChanges();

			return result;
		}

		private static (XmlSchemaSet, List<XSElement>) ReadSchemata(CmsDbContext dbContext)
		{
			IReadOnlyList<Schema> schemata = dbContext.Schemata.AsNoTracking().ToArray();

			if (schemata.Count == 0)
				schemata = CreateDefault(dbContext);

			return ReadSchemata(schemata);
		}

		public static (XmlSchemaSet, List<XSElement>) ReadSchemata(IReadOnlyList<Schema> schemata)
		{
			XmlSchemaSet schemaSet = new();

			for (int i = 0; i < schemata.Count; i++)
			{
				var schema = ReadXsd(schemata[i].Data);
				schemaSet.Add(schema);
			}

			schemaSet.Compile();

			List<XSElement> elements = [];
			XSElement e;

			foreach (XmlSchema compiledSchema in schemaSet.Schemas())
				foreach (XmlSchemaElement element in compiledSchema.Elements.Values)
					if ((e = TraverseSchema(element, null)) != null)
						elements.Add(e);

			//string json = JsonSerializer.Serialize(elements, options: new JsonSerializerOptions() { WriteIndented = true });
			//Console.WriteLine(json);

			return (schemaSet, elements);
		}

		public bool Reload(CmsDbContext dbContext)
		{
			bool result;

			try
			{
				var (schemaSet, fragments) = ReadSchemata(dbContext);
				var index = new Dictionary<string, XSElement>();

				foreach (var f in fragments)
					f.AddSelfToIndex(index);

				SchemaSet = schemaSet;
				Fragments = fragments;
				Index = index;

				result = true;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				result = false;
			}

			return result;
		}

		public bool Reload(XmlSchemaSet schemaSet, List<XSElement> fragments)
		{
			var index = new Dictionary<string, XSElement>();

			foreach (var f in fragments)
				f.AddSelfToIndex(index);

			SchemaSet = schemaSet;
			Fragments = fragments;
			Index = index;

			//foreach (XmlSchema s in schemaSet.Schemas())
			//	s.TargetNamespace;

			return true;
		}

		public static StringBuilder GetDefaultValue(XSElement xse)
		{
			Dictionary<string, string> NamespacesPrefixes = [];
			xse.AddSelfNamespaces(NamespacesPrefixes, "");

			StringBuilder result = new();

			void getDefaultValue(XSElement e, int level)
			{
				int n = e.MinOccurs;

				if (n == 0)
				{
					result
						.AppendLine("<!-- Do not remove the comment below -->")
						.AppendLine(string.Format("<!-- {0} -->", e.Path));

					return; // n = 1;
				}

				if (NamespacesPrefixes.TryGetValue(e.Namespace, out string prefix) && !string.IsNullOrEmpty(prefix))
					prefix += ":";

				string indent = new('\t', level);

				for (int i = 0; i < n; i++)
				{
					result
						.Append(indent)
						.Append('<');

					result.Append(prefix + e.Name);

					if (e.IsSimple)
					{
						result.Append('>').Append(e.DefaultXmlValue());
					}
					else
					{
						result.AppendLine(">");

						for (int j = 0; j < e.Elements.Count; j++)
							getDefaultValue(e.Elements[j], level + 1);

						result.Append(indent);
					}

					result.AppendLine(string.Format("</{0}{1}>", prefix, e.Name));
				}
			}

			result
				.Append('<')
				.Append(xse.Name)
				.AppendLine()
				.AppendFormat("\txmlns=\"{0}\"", xse.Namespace);

			if (NamespacesPrefixes.Count > 1)
				foreach (var ns in NamespacesPrefixes)
					if (!string.IsNullOrEmpty(ns.Value))
						result.AppendLine().AppendFormat("\txmlns:{0}=\"{1}\"", ns.Value, ns.Key);
			
			result.Append('>');

			if (xse.Elements != null)
			{
				result.AppendLine();

				for (int i = 0; i < xse.Elements.Count; i++)
					getDefaultValue(xse.Elements[i], 1);
			}
			else
				result.Append(xse.DefaultXmlValue());

			result.AppendLine(string.Format("</{0}>", xse.Name));

			return result;
		}

		public XSElement Find(string path)
		{
			if (Index.TryGetValue(path, out XSElement result))
				return result;

			return null;
		}

		/*
		public void Traverse(string xmlName, Func<XSElement, int, int, int> callback)
		{
			var xse = Find(xmlName);
			int ofs = 0;

			void traverse(XSElement xse, int level, int parentIdx, Func<XSElement, int, int, int> callback)
			{
				XSElement e;

				for (int i = 0; i < xse.Elements.Count; i++)
				{
					ofs++;

					if (callback(e = xse.Elements[i], level, parentIdx) == 0 && e.Elements != null)
						traverse(e, level + 1, ofs, callback);
				}
			}

			if (xse != null && callback(xse, 0, -1) == 0 && xse.Elements != null)
				traverse(xse, 1, ofs, callback);
		}
		*/
	}
}