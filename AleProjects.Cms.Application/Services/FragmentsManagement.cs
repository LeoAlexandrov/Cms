using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using Ganss.Xss;

using AleProjects.Cms.Application.Dto;
using AleProjects.Cms.Domain.Entities;
using AleProjects.Cms.Domain.ValueObjects;


namespace AleProjects.Cms.Application.Services
{

	public partial class ContentManagementService
	{
		class LinkComparer : IComparer<FragmentLink>
		{
			public int Compare(FragmentLink x, FragmentLink y)
			{
				if (ReferenceEquals(x, y)) 
					return 0;

				if (x is null)
					return 1;

				if (y is null) 
					return -1;

				return x.ContainerRef.CompareTo(y.ContainerRef);
			}
		}

		#region private-functions

		private static void RecursiveDelete(FragmentLink[] links, int containerRef, LinkComparer linkComparer, List<Fragment> fragmentsToDelete, List<FragmentLink> linksToDelete)
		{
			FragmentLink searchLink = new() { ContainerRef = containerRef };
			int idx = Array.BinarySearch(links, searchLink, linkComparer);

			if (idx < 0)
				return;

			while (idx > 0 && links[idx - 1].ContainerRef == containerRef)
				idx--;

			for (int i = idx; i < links.Length && links[i].ContainerRef == containerRef; i++)
			{
				if (links[i].Fragment.Shared)
					linksToDelete.Add(links[i]);
				else
					fragmentsToDelete.Add(links[i].Fragment);

				RecursiveDelete(links, links[i].Id, linkComparer, fragmentsToDelete, linksToDelete);
			}
		}

		private static List<FragmentLink> RecursiveSelect(FragmentLink[] links, int containerRef, LinkComparer linkComparer, List<FragmentLink> children)
		{
			FragmentLink searchLink = new() { ContainerRef = containerRef };
			int idx = Array.BinarySearch(links, searchLink, linkComparer);

			if (idx < 0)
				return children;

			while (idx > 0 && links[idx - 1].ContainerRef == containerRef)
				idx--;

			for (int i = idx; i < links.Length && links[i].ContainerRef == containerRef; i++)
			{
				children.Add(links[i]);

				RecursiveSelect(links, links[i].Id, linkComparer, children);
			}

			return children;
		}

		private static List<DtoFragmentElement> TraverseElement(XmlNode element, int level, string path, string lang, List<DtoFragmentElement> list, IDictionary<string, XSElement> index)
		{
			int k = element.Name.IndexOf(':');
			string name = element.Name[(k + 1)..];
			string ns = element.NamespaceURI;

			if (string.IsNullOrEmpty(path))
				path = ns + ":" + name;
			else
				path += "\\" + ns + ":" + name;


			XSElement xse = index[path];

			DtoFragmentElement vmi = new(xse, lang)
			{
				Level = level,
				Path = path,
			};

			list.Add(vmi);

			XmlNode node = element.FirstChild;

			if (node != null)
			{
				var sb = new StringBuilder(512);

				do
				{
					if (node.NodeType == XmlNodeType.Element)
						TraverseElement(node, level + 1, path, lang, list, index);
					else if (node.NodeType == XmlNodeType.Text || node.NodeType == XmlNodeType.CDATA)
						sb.Append(node.InnerText.Trim());
					else if (node.NodeType == XmlNodeType.Comment)
					{
						string nodePath = node.InnerText.Trim();

						if (index.TryGetValue(nodePath, out xse))
						{
							DtoFragmentElement vmi2 = new(xse, lang)
							{
								Level = level + 1,
								Path = nodePath,
								IsAddable = true,
								Value = xse.DefaultObjectValue()
							};

							list.Add(vmi2);
						}
					}

					node = node.NextSibling;

				} while (node != null);

				switch (vmi.XmlType)
				{
					case "boolean":
						if (bool.TryParse(sb.ToString(), out bool b))
							vmi.Value = b;

						break;

					case "integer":
					case "int":
					case "short":
					case "byte":
						if (int.TryParse(sb.ToString(), out int i))
							vmi.Value = i;

						break;

					case "decimal":
					case "double":
					case "float":
						if (double.TryParse(sb.ToString(), out double d))
							vmi.Value = d;

						break;

					default:
						vmi.Value = sb.ToString();
						break;
				}
			}
			else if (vmi.XmlType == "string" ||
				(vmi.XmlType != "boolean" &&
				 vmi.XmlType != "integer" && vmi.XmlType != "int" && vmi.XmlType != "short" && vmi.XmlType != "byte" &&
				 vmi.XmlType != "decimal" && vmi.XmlType != "double" && vmi.XmlType != "float"))
			{
				vmi.Value = string.Empty;
			}

			return list;
		}

		private static string BuildXmlValue(IReadOnlyList<DtoFragmentElement> decomposition, HtmlSanitizer sanitizer)
		{
			if (decomposition == null || decomposition.Count == 0)
				return null;

			Dictionary<string, string> NamespacesPrefixes = [];
			string prefix = "";

			for (int i = 0; i < decomposition.Count; i++)
				if (NamespacesPrefixes.TryAdd(decomposition[i].Namespace, prefix))
					prefix = XSElement.NextNSPrefix(prefix);


			StringBuilder result = new();

			result
				.Append('<')
				.Append(decomposition[0].Name)
				.AppendLine()
				.Append($"\txmlns=\"{decomposition[0].Namespace}\"");

			if (NamespacesPrefixes.Count > 1)
				foreach (var ns in NamespacesPrefixes)
					if (!string.IsNullOrEmpty(ns.Value))
						result.AppendLine().Append($"\txmlns:{ns.Value}=\"{ns.Key}\"");

			result.Append('>');

			if (decomposition.Count > 1)
				result.AppendLine();


			Stack<string> closeTags = new();
			IFormatProvider fmt = new NumberFormatInfo();
			string tag;
			int level = 0;

			closeTags.Push(decomposition[0].Name);

			for (int i = 1; i < decomposition.Count; i++)
			{
				if (decomposition[i].IsAddable)
				{
					if (decomposition[i].Level < level)
					{
						do
						{
							level--;
							tag = closeTags.Pop();

							result
								.Append('\t', level)
								.Append($"</{tag}>")
								.AppendLine();
						} while (level > decomposition[i].Level);
					}

					result
						.AppendLine("<!-- Do not remove the comment below -->")
						.AppendLine($"<!-- {decomposition[i].Path} -->");
				}
				else if (decomposition[i].IsSimple)
				{
					if (decomposition[i].Level < level)
					{
						do
						{
							level--;
							tag = closeTags.Pop();

							result
								.Append('\t', level)
								.Append($"</{tag}>")
								.AppendLine();
						} while (level > decomposition[i].Level);
					}

					if (NamespacesPrefixes.TryGetValue(decomposition[i].Namespace, out tag) && !string.IsNullOrEmpty(tag))
						tag += ":" + decomposition[i].Name;
					else
						tag = decomposition[i].Name;

					result
						.Append('\t', decomposition[i].Level)
						.Append($"<{tag}>");

					switch (decomposition[i].Value)
					{
						case string s:

							if (sanitizer != null)
								s = sanitizer.Sanitize(s);

							if (s.Any(c => c == '<' || c == '>' || c == '"' || c == '\'' || c == '&' || c == '\r' || c == '\n' || c == '\t'))
								result.AppendLine($"<![CDATA[{s}]]></{tag}>");
							else
								result.AppendLine($"{s}</{tag}>");

							break;

						case bool b:
							result.AppendLine($"{(b ? "true" : "false")}</{tag}>");
							break;

						default:
							result
								.AppendFormat(fmt, "{0}</{1}>", decomposition[i].Value, tag)
								.AppendLine();
							break;
					}
				}
				else if (decomposition[i].Level < level)
				{
					do
					{
						level--;
						tag = closeTags.Pop();

						result
							.Append('\t', level)
							.Append($"</{tag}>")
							.AppendLine();
					} while (level > decomposition[i].Level);

					if (NamespacesPrefixes.TryGetValue(decomposition[i].Namespace, out tag) && !string.IsNullOrEmpty(tag))
						tag += ":" + decomposition[i].Name;
					else
						tag = decomposition[i].Name;

					result
						.Append('\t', decomposition[i].Level)
						.Append($"<{tag}>")
						.AppendLine();

					closeTags.Push(tag);
				}
				else
				{
					if (NamespacesPrefixes.TryGetValue(decomposition[i].Namespace, out tag) && !string.IsNullOrEmpty(tag))
						tag += ":" + decomposition[i].Name;
					else
						tag = decomposition[i].Name;

					result
						.Append('\t', decomposition[i].Level)
						.Append($"<{tag}>")
						.AppendLine();

					closeTags.Push(tag);
				}

				level = decomposition[i].Level;
			}

			level = closeTags.Count;

			while (level > 0)
			{
				tag = closeTags.Pop();
				level--;

				if (level == 0 && decomposition[0].Value != null)
				{
					string val = decomposition[0].Value.ToString();

					if (!string.IsNullOrEmpty(val))
					{
						if (sanitizer != null)
							val = sanitizer.Sanitize(val);

						if (val.Any(c => c == '<' || c == '>' || c == '"' || c == '\'' || c == '&' || c == '\r' || c == '\n' || c == '\t'))
							result.Append($"<![CDATA[{val}]]>");
						else
							result.Append(val);
					}
				}

				result
					.Append('\t', level)
					.Append($"</{tag}>")
					.AppendLine();
			}

			return result.ToString();
		}

		private static StringBuilder GetDefaultValue(XSElement xse)
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
						.AppendLine("<!-- Do not remove or change the comment below -->")
						.AppendLine($"<!-- {e.Path} -->");

					return;
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

					result.AppendLine($"</{prefix}{e.Name}>");
				}
			}

			result
				.Append('<')
				.Append(xse.Name)
				.AppendLine()
				.Append($"\txmlns=\"{xse.Namespace}\"");

			if (NamespacesPrefixes.Count > 1)
				foreach (var ns in NamespacesPrefixes)
					if (!string.IsNullOrEmpty(ns.Value))
						result.AppendLine().Append($"\txmlns:{ns.Value}=\"{ns.Key}\"");

			result.Append('>');

			if (xse.Elements != null)
			{
				result.AppendLine();

				for (int i = 0; i < xse.Elements.Count; i++)
					getDefaultValue(xse.Elements[i], 1);
			}
			else
				result.Append(xse.DefaultXmlValue());

			result.AppendLine($"</{xse.Name}>");

			return result;
		}

		private static List<DtoFragmentElement> AddDefaultValue(XSElement element, int level, string lang, List<DtoFragmentElement> list)
		{
			if (element.MinOccurs == 0)
			{
				DtoFragmentElement vmi = new(element, lang)
				{
					Level = level,
					Path = element.Path,
					Value = element.DefaultObjectValue(),
					IsAddable = true
				};

				list.Add(vmi);
			}
			else
			{
				for (int i = 0; i < element.MinOccurs; i++)
				{
					DtoFragmentElement vmi = new(element, lang)
					{
						Level = level,
						Path = element.Path,
						Value = element.DefaultObjectValue()
					};

					int pIdx = list.Count;

					list.Add(vmi);

					if (!element.IsSimple)
						for (int j = 0; j < element.Elements.Count; j++)
							AddDefaultValue(element.Elements[j], level + 1, lang, list);
				}
			}

			return list;
		}

		private static DtoFragmentLiteResult[] FragmentTemplates(List<XSElement> fragments, string language)
		{
			if (fragments == null)
				return [];

			var result = fragments
				.Select(f => new DtoFragmentLiteResult() { Value = f.Name, Label = f.GetAnnotationDoc(language), Ns = f.Namespace })
				.OrderBy(f => f.Label)
				.ToArray();

			return result;
		}

		#endregion

		public async Task<DtoFragmentLiteResult[]> SharedFragments()
		{
			var fragments = await dbContext.Fragments
				.AsNoTracking()
				.Where(f => f.Shared)
				.Select(f => new DtoFragmentLiteResult() { Label = f.Name, Value = f.Id.ToString(), Ns = f.XmlSchema })
				.OrderBy(f => f.Label)
				.ToArrayAsync();

			return fragments;
		}

		public async Task<DtoFragmentCreationStuffResult> FragmentCreationStuff(string language)
		{
			var shared = await this.SharedFragments();
			var templates = FragmentTemplates(_schemaRepo.Fragments, language);

			return new() { Shared = shared, Templates = templates };
		}

		public async Task<Result<DtoFullFragmentResult>> GetFragmentByLink(int id, string lang)
		{
			var link = await dbContext.FragmentLinks.FindAsync(id);

			if (link == null)
				return Result<DtoFullFragmentResult>.NotFound();

			var fragment = await dbContext.Fragments.FindAsync(link.FragmentRef);

			int useCount = 0;

			if (fragment.Shared)
				useCount = await dbContext.FragmentLinks
					.Where(fl => fl.DocumentRef != link.DocumentRef && fl.FragmentRef == link.FragmentRef)
					.CountAsync();

			int fId = fragment.Id;

			FragmentAttribute[] attrs = await dbContext.FragmentAttributes
				.AsNoTracking()
				.Where(a => a.FragmentRef == fId)
				.OrderBy(a => a.AttributeKey)
				.ToArrayAsync();


			DtoFullFragmentResult dto = new() 
			{ 
				Properties = new(fragment), 
				Enabled = link.Enabled, 
				Anchor = link.Anchor,
				LockShare = useCount > 0,  
				LinkId = link.Id,
				ContainerRef = link.ContainerRef,
				Attributes = attrs.Select(a => new DtoFragmentAttributeResult(a)).ToArray(),
				RawXml = fragment.Data
			};
			
			XmlReaderSettings settings = new() { ValidationType = ValidationType.Schema };

			settings.Schemas.Add(_schemaRepo.SchemaSet);

			using MemoryStream xmlStream = new(Encoding.UTF8.GetBytes(fragment.Data));
			using XmlReader reader = XmlReader.Create(xmlStream, settings);


			/*
			XPathDocument xpathDoc = new(reader);
			XPathNavigator xpathNav = xpathDoc.CreateNavigator();
			XmlNamespaceManager manager = new(xpathNav.NameTable);

			manager.AddNamespace("t", fragment.XmlSchema);
			*/

			try
			{
				XmlDocument document = new();
				document.Load(reader);

				var root = document.DocumentElement;

				if (root != null)
				{
					dto.Decomposition = TraverseElement(root, 0, null, lang, [], _schemaRepo.Index);
				}
				else
				{
					throw new XmlException();
				}
			}
			catch (Exception ex)
			{
				dto.Decomposition = [];
				dto.ValidationError = ex.Message;
			}

			/*
			List<DtoFragmentElement> decomposition = [];

			int callback(XSElement e, int level, int parentIdx)
			{
				var nodes = xpathNav.Select(e.XmlPath("t"), manager);

				if (nodes.Count != 0)
				{
					if (nodes.Count > 1)
					{
						Console.WriteLine("aaaa");
					}

					Console.WriteLine(e.XmlPath("t") + " | " + level.ToString() + " | " + parentIdx.ToString());

					decomposition.Add(new()
					{
						Level = level,
						ParentIndex = parentIdx,
						Path = e.Path,
					});

					return 0;
				}
				else
				{
					decomposition.Add(new() 
					{ 
						IsEmptyHolder = true,
						Level = level,
						ParentIndex = parentIdx,
						Path = e.Path,
					});

					return 1;
				}

			}

			_schemaService.Traverse(fragment.XmlName, callback);
			*/

			return Result<DtoFullFragmentResult>.Success(dto);
		}

		public async Task<Result<DtoFragmentChangeResult>> CreateFragment(DtoCreateFragment dto, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, dto.Document, "CanManageDocument");

			if (!authResult.Succeeded)
				return Result<DtoFragmentChangeResult>.Forbidden();

			string name;

			authResult = await _authService.AuthorizeAsync(user, "NoInputSanitizing");

			if (!authResult.Succeeded)
			{
				var sanitizer = new HtmlSanitizer();

				name = sanitizer.Sanitize(dto.Name);
			}
			else
			{
				name = dto.Name;
			}


			Document doc = await dbContext.Documents.FindAsync(dto.Document);

			if (doc == null)
				return Result<DtoFragmentChangeResult>.BadParameters("Document", "No document found");

			FragmentLink parent;
			int position;

			if (dto.Parent > 0)
			{
				parent = await dbContext.FragmentLinks.FindAsync(dto.Parent);

				if (parent == null)
					return Result<DtoFragmentChangeResult>.BadParameters("Parent", "No parent fragment found");

				if (parent.DocumentRef != dto.Document)
					return Result<DtoFragmentChangeResult>.BadParameters("Document", "Invalid document specified");

				position = await dbContext.FragmentLinks.CountAsync(d => d.ContainerRef == dto.Parent);
			}
			else
			{
				parent = null;
				position = await dbContext.FragmentLinks.CountAsync(d => d.ContainerRef == 0 && d.DocumentRef == doc.Id);
			}

			Fragment sharedFragment;
			Fragment fr;
			FragmentLink fl;
			string content;
			string xmlName;

			if (dto.SharedFragment != null)
			{
				sharedFragment = await dbContext.Fragments.FindAsync(int.Parse(dto.SharedFragment));

				if (sharedFragment == null)
					return Result<DtoFragmentChangeResult>.BadParameters("SharedFragment", "No shared fragment found");

				content = null;
				xmlName = null;

				fr = sharedFragment;

				fl = new()
				{
					FragmentRef = fr.Id,
					ContainerRef = dto.Parent,
					DocumentRef = dto.Document,
					Position = position,
					Enabled = true
				};

				dbContext.FragmentLinks.Add(fl);
			}
			else
			{
				sharedFragment = null;
				content = null;
				xmlName = null;

				for (int i = 0; i < _schemaRepo.Fragments.Count; i++)
					if (_schemaRepo.Fragments[i].Name == dto.TemplateName &&
						_schemaRepo.Fragments[i].Namespace == dto.Schema)
					{
						var sb = GetDefaultValue(_schemaRepo.Fragments[i]);
						content = sb.ToString();
						xmlName = _schemaRepo.Fragments[i].Name;
						sb.Clear();
						break;
					}

				fl = new()
				{
					ContainerRef = dto.Parent,
					DocumentRef = dto.Document,
					Position = position,
					Enabled = true
				};

				fr = new()
				{
					Name = name,
					Data = content,
					XmlName = xmlName,
					XmlSchema = dto.Schema,
					DocumentLinks = [fl]
				};

				dbContext.Fragments.Add(fr);
			}

			XSElement xse;

			if ((xse = _schemaRepo.Find(fr.XmlSchema + ":" + fr.XmlName)) != null && xse.RepresentsContainer)
				fl.Data = "container";


			doc.ModifiedAt = DateTimeOffset.UtcNow;
			doc.Author = user.Identity.Name;


			var existingRefs = await dbContext.References
				.Where(r => r.DocumentRef == dto.Document)
				.OrderBy(r => r.ReferenceTo)
				.ThenBy(r => r.MediaLink)
				.ToListAsync();

			string[] xmlData = await dbContext.Fragments
				.Join(dbContext.FragmentLinks, f => f.Id, fl => fl.FragmentRef, (f, fl) => new { fl.Id, fl.DocumentRef, fl.Enabled, f.Data })
				.Where(f => f.DocumentRef == dto.Document && f.Enabled)
				.Select(f => f.Data)
				.ToArrayAsync();

			ReferencesHelper.GetReferencesChanges(dto.Document,
				existingRefs,
				ReferencesHelper.Extract([doc.Summary, doc.CoverPicture, fr.Data, .. xmlData]),
				out List<Reference> toAdd,
				out List<Reference> toRemove);

			if (toAdd != null)
				dbContext.References.AddRange(toAdd);

			if (toRemove != null)
				dbContext.References.RemoveRange(toRemove);


			await dbContext.SaveChangesAsync();

			await _notifier.Notify("on_doc_update", doc.RootSlug, doc.Path, doc.Id);

			return Result<DtoFragmentChangeResult>.Success(
				new DtoFragmentChangeResult()
				{
					Fragment = new(fr),
					Link = new(fl),
					Author = doc.Author,
					ModifiedAt = doc.ModifiedAt
				});
		}

		public async Task<Result<DtoFragmentChangeResult>> UpdateFragmentByLink(int id, DtoFullFragment dto, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, id, "CanManageFragment");

			if (!authResult.Succeeded)
				return Result<DtoFragmentChangeResult>.Forbidden();

			if (!string.IsNullOrEmpty(dto.RawXml))
			{
				authResult = await _authService.AuthorizeAsync(user, "IsAdmin");

				if (!authResult.Succeeded)
					return Result<DtoFragmentChangeResult>.Forbidden();
			}

			var link = await dbContext.FragmentLinks.FindAsync(id);

			if (link == null)
				return Result<DtoFragmentChangeResult>.NotFound();

			
			authResult = await _authService.AuthorizeAsync(user, "NoInputSanitizing");

			HtmlSanitizer sanitizer = authResult.Succeeded ? null : new();

			string data = string.IsNullOrEmpty(dto.RawXml) ?
				BuildXmlValue(dto.Decomposition, sanitizer) :
				sanitizer?.Sanitize(dto.RawXml) ?? dto.RawXml;

			XmlReaderSettings settings = new() { ValidationType = ValidationType.Schema };
			XmlDocument document = new();

			settings.Schemas.Add(_schemaRepo.SchemaSet);

			using MemoryStream xmlStream = new(Encoding.UTF8.GetBytes(data));
			using XmlReader reader = XmlReader.Create(xmlStream, settings);

			try
			{
				document.Load(reader);
			}
			catch
			{
				return Result<DtoFragmentChangeResult>.BadParameters("Decomposition", "Bad xml content");
			}


			if (link.Enabled != dto.Enabled)
				link.Enabled = dto.Enabled;

			if (link.Anchor != dto.Anchor)
			{
				link.Anchor = dto.Anchor;

				var links = await dbContext.FragmentLinks
					.Where(l => l.DocumentRef == link.DocumentRef)
					.OrderBy(l => l.ContainerRef)
					.ThenBy(l => l.Position)
					.ToArrayAsync();

				if (dto.Anchor)
				{
					FragmentLink parent = links.FirstOrDefault(l => l.Id == link.ContainerRef);

					while (parent != null)
					{
						parent.Anchor = true;
						parent = links.FirstOrDefault(l => l.Id == parent.ContainerRef);
					}
				}
				else
				{
					List<FragmentLink> sublinks = RecursiveSelect(links, link.Id, new LinkComparer(), []);

					sublinks.ForEach(l => l.Anchor = false);
				}
			}

			var fragment = await dbContext.Fragments.FindAsync(link.FragmentRef);

			if (sanitizer != null)
				fragment.Name = sanitizer.Sanitize(dto.Properties.Name);
			else
				fragment.Name = dto.Properties.Name;


			if (fragment.Shared)
			{
				var useCount = await dbContext.FragmentLinks
					.Where(fl => fl.DocumentRef != link.DocumentRef && fl.FragmentRef == link.FragmentRef)
					.CountAsync();

				if (!dto.Properties.Shared && useCount > 0)
					return Result<DtoFragmentChangeResult>.BadParameters("Shared", "Fragment can't be unshared");
			}

			bool sharedStateChanged = fragment.Shared != dto.Properties.Shared;

			fragment.Shared = dto.Properties.Shared;
			fragment.Data = data;

			XSElement xse;

			if ((xse = _schemaRepo.Find(fragment.XmlSchema + ":" + fragment.XmlName)) != null && xse.RepresentsContainer)
				link.Data = "container";


			Document doc = await dbContext.Documents.FindAsync(link.DocumentRef);

			doc.ModifiedAt = DateTimeOffset.UtcNow;
			doc.Author = user.Identity.Name;


			var existingRefs = await dbContext.References
				.Where(r => r.DocumentRef == doc.Id)
				.OrderBy(r => r.ReferenceTo)
				.ThenBy(r => r.MediaLink)
				.ToListAsync();

			string[] xmlData = await dbContext.Fragments
				.Join(dbContext.FragmentLinks, f => f.Id, fl => fl.FragmentRef, (f, fl) => new { fl.Id, fl.DocumentRef, fl.Enabled, f.Data })
				.Where(f => f.DocumentRef == doc.Id && f.Id != id && f.Enabled)
				.Select(f => f.Data)
				.ToArrayAsync();

			ReferencesHelper.GetReferencesChanges(doc.Id,
				existingRefs,
				ReferencesHelper.Extract([doc.Summary, doc.CoverPicture, dto.Enabled ? data : null, .. xmlData]),
				out List<Reference> toAdd,
				out List<Reference> toRemove);

			if (toAdd != null)
				dbContext.References.AddRange(toAdd);

			if (toRemove != null)
				dbContext.References.RemoveRange(toRemove);


			await dbContext.SaveChangesAsync();

			await _notifier.Notify("on_doc_update", doc.RootSlug, doc.Path, doc.Id);

			return Result<DtoFragmentChangeResult>.Success(
				new DtoFragmentChangeResult()
				{
					Fragment = new(fragment), 
					Link = new(link), 
					SharedStateChanged = sharedStateChanged, 
					Author = doc.Author,
					ModifiedAt = doc.ModifiedAt
				});
		}

		public async Task<Result<DtoDocumentChangeResult>> DeleteFragmentByLink(int id, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, id, "CanManageFragment");

			if (!authResult.Succeeded)
				return Result<DtoDocumentChangeResult>.Forbidden();

			var link = await dbContext.FragmentLinks.FindAsync(id);

			if (link == null)
				return Result<DtoDocumentChangeResult>.NotFound();

			int containerRef = link.ContainerRef;
			int docId = link.DocumentRef;

			var links = await dbContext.FragmentLinks
				.Where(l => l.DocumentRef == docId)
				.OrderBy(l => l.ContainerRef)
				.ThenBy(l => l.Position)
				.Include(l => l.Fragment)
				.ToArrayAsync();


			List<Fragment> fragmentsToDelete = new(links.Length + 1);
			List<FragmentLink> linksToDelete = new(links.Length + 1);

			var fragment = await dbContext.Fragments.FindAsync(link.FragmentRef);

			if (fragment != null)
				if (fragment.Shared)
				{
					// check if it is the last used shared fragment and delete it too
					linksToDelete.Add(link);
				}
				else
					fragmentsToDelete.Add(fragment);

			LinkComparer linkComparer = new();

			RecursiveDelete(links, id, linkComparer, fragmentsToDelete, linksToDelete);

			int idx = Array.FindIndex(links, l => l.Id == id);

			if (idx > 0)
				while (idx < links.Length && links[idx].ContainerRef == containerRef)
					links[idx++].Position--;

			dbContext.Fragments.RemoveRange(fragmentsToDelete);
			dbContext.FragmentLinks.RemoveRange(linksToDelete);

			var doc = await dbContext.Documents.FindAsync(docId);

			doc.ModifiedAt = DateTimeOffset.UtcNow;
			doc.Author = user.Identity.Name;


			var existingRefs = await dbContext.References
				.Where(r => r.DocumentRef == docId)
				.OrderBy(r => r.ReferenceTo)
				.ThenBy(r => r.MediaLink)
				.ToListAsync();

			int[] excludedIds = RecursiveSelect(links, id, linkComparer, [])
				.Select(l => l.Id)
				.ToArray();

			string[] xmlData = await dbContext.Fragments
				.Join(dbContext.FragmentLinks, f => f.Id, fl => fl.FragmentRef, (f, fl) => new { fl.Id, fl.DocumentRef, fl.Enabled, f.Data })
				.Where(f => f.DocumentRef == docId && f.Id != id && !excludedIds.Contains(f.Id) && f.Enabled)
				.Select(f => f.Data)
				.ToArrayAsync();

			ReferencesHelper.GetReferencesChanges(docId,
				existingRefs,
				ReferencesHelper.Extract([doc.Summary, doc.CoverPicture, .. xmlData]),
				out List<Reference> toAdd,
				out List<Reference> toRemove);

			if (toAdd != null)
				dbContext.References.AddRange(toAdd);

			if (toRemove != null)
				dbContext.References.RemoveRange(toRemove);


			await dbContext.SaveChangesAsync();

			await _notifier.Notify("on_doc_update", doc.RootSlug, doc.Path, doc.Id);

			return Result<DtoDocumentChangeResult>.Success(new() { Author = doc.Author, ModifiedAt = doc.ModifiedAt });
		}

		public async Task<Result<DtoMoveFragmentResult>> MoveFragment(int id, int posIncrement, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, id, "CanManageFragment");

			if (!authResult.Succeeded)
				return Result<DtoMoveFragmentResult>.Forbidden();

			var link = await dbContext.FragmentLinks.FindAsync(id);

			if (link == null)
				return Result<DtoMoveFragmentResult>.NotFound();

			int containerRef = link.ContainerRef;
			int docId = link.DocumentRef;

			var doc = await dbContext.Documents.FindAsync(docId);

			var siblings = await dbContext.FragmentLinks
				.Where(l => l.ContainerRef == containerRef && l.DocumentRef == docId)
				.OrderBy(d => d.Position)
				.ToArrayAsync();

			int oldPosition = link.Position;
			int newPosition = oldPosition + posIncrement;

			if (newPosition < 0)
				newPosition = 0;
			else if (newPosition >= siblings.Length)
				newPosition = siblings.Length - 1;

			if (newPosition != oldPosition)
			{
				link.Position = newPosition;

				if (posIncrement < 0)
				{
					for (int i = newPosition; i < oldPosition; i++)
						siblings[i].Position++;
				}
				else
				{
					for (int i = oldPosition + 1; i <= newPosition; i++)
						siblings[i].Position--;
				}

				doc.ModifiedAt = DateTimeOffset.UtcNow;
				doc.Author = user.Identity.Name;

				await dbContext.SaveChangesAsync();

				await _notifier.Notify("on_doc_update", doc.RootSlug, doc.Path, doc.Id);
			}

			return Result<DtoMoveFragmentResult>.Success(
				new DtoMoveFragmentResult()
				{
					NewPosition = newPosition,
					OldPosition = oldPosition,
					Author = doc.Author,
					ModifiedAt = doc.ModifiedAt
				});
		}

		public async Task<Result<DtoFragmentChangeResult>> CopyFragment(int id, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, id, "CanManageFragment");

			if (!authResult.Succeeded)
				return Result<DtoFragmentChangeResult>.Forbidden();

			var link = await dbContext.FragmentLinks.FindAsync(id);

			if (link == null)
				return Result<DtoFragmentChangeResult>.NotFound();

			var fragment = await dbContext.Fragments.FindAsync(link.FragmentRef);

			if (fragment == null)
				return Result<DtoFragmentChangeResult>.NotFound();

			int documentRef = link.DocumentRef;
			int containerRef = link.ContainerRef;
			int fId = fragment.Id;

			FragmentAttribute[] attrs = await dbContext.FragmentAttributes
				.AsNoTracking()
				.Where(a => a.FragmentRef == fId)
				.OrderBy(a => a.AttributeKey)
				.ToArrayAsync();

			int pos = await dbContext.FragmentLinks.CountAsync(l => l.ContainerRef == containerRef && l.DocumentRef == documentRef);
			var doc = await dbContext.Documents.FindAsync(documentRef);

			var newFragment = new Fragment()
			{
				Name = fragment.Name + " (copy)",
				XmlSchema = fragment.XmlSchema,
				XmlName = fragment.XmlName,
				Data = fragment.Data,
				FragmentAttributes = attrs.Select(a => new FragmentAttribute() { AttributeKey = a.AttributeKey, Enabled = a.Enabled, Value = a.Value }).ToList()
			};

			var newLink = new FragmentLink()
			{
				DocumentRef = documentRef,
				ContainerRef = containerRef,
				Position = pos,
				Enabled = link.Enabled,
				Anchor = link.Anchor,
				Fragment = newFragment
			};

			dbContext.FragmentLinks.Add(newLink);

			XSElement xse;

			if ((xse = _schemaRepo.Find(fragment.XmlSchema + ":" + fragment.XmlName)) != null && xse.RepresentsContainer)
				newLink.Data = "container";

			doc.ModifiedAt = DateTimeOffset.UtcNow;
			doc.Author = user.Identity.Name;

			await dbContext.SaveChangesAsync();

			await _notifier.Notify("on_doc_update", doc.RootSlug, doc.Path, doc.Id);

			return Result<DtoFragmentChangeResult>.Success(
				new DtoFragmentChangeResult()
				{
					Fragment = new(newFragment),
					Link = new(newLink),
					Author = doc.Author,
					ModifiedAt = doc.ModifiedAt
				});
		}

		public async Task<Result<DtoDocumentChangeResult>> SetFragmentContainer(int id, int linkId, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, id, "CanManageFragment");

			if (!authResult.Succeeded)
				return Result<DtoDocumentChangeResult>.Forbidden();

			var link = await dbContext.FragmentLinks.FindAsync(id);

			if (link == null)
				return Result<DtoDocumentChangeResult>.NotFound();

			int containerRef = link.ContainerRef;
			int docId = link.DocumentRef;

			var doc = await dbContext.Documents.FindAsync(docId);

			if (containerRef == linkId)
				return Result<DtoDocumentChangeResult>.Success(new() { Author = doc.Author, ModifiedAt = doc.ModifiedAt });


			if (linkId != 0)
			{
				var newLink = await dbContext.FragmentLinks
					.AsNoTracking()
					.Include(l => l.Fragment)
					.Where(l => l.Id == linkId)
					.FirstOrDefaultAsync();

				if (newLink == null)
					return Result<DtoDocumentChangeResult>.BadParameters("Container", "New container not found");

				if (newLink.DocumentRef != docId)
					return Result<DtoDocumentChangeResult>.BadParameters("Container", "New container must be in the same document");

				// check if the new container is "self-nested"

				if (newLink.ContainerRef != 0)
				{
					var docLinks = await dbContext.FragmentLinks
						.AsNoTracking()
						.Where(l => l.DocumentRef == docId)
						.OrderBy(l => l.Id)
						.Select(l => new { l.Id, l.ContainerRef })
						.ToArrayAsync();

					int newLinkIdx = Array.FindIndex(docLinks, l => l.Id == linkId);

					if (newLinkIdx >= 0)
					{
						int cRef = docLinks[newLinkIdx].ContainerRef;

						do
						{
							if (cRef == id)
								return Result<DtoDocumentChangeResult>.BadParameters("Container", "New container is invalid");

							newLinkIdx = Array.FindIndex(docLinks, l => l.Id == cRef);
							cRef = newLinkIdx < 0 ? cRef = 0 : docLinks[newLinkIdx].ContainerRef;
						}
						while (cRef != 0);
					}
				}

				XSElement xse;

				if ((xse = _schemaRepo.Find(newLink.Fragment.XmlSchema + ":" + newLink.Fragment.XmlName)) != null && !xse.RepresentsContainer)
					return Result<DtoDocumentChangeResult>.BadParameters("Container", "New container is invalid");
			}


			int oldPosition = link.Position;

			var siblings = await dbContext.FragmentLinks
				.Where(l => l.ContainerRef == containerRef && l.DocumentRef == docId && l.Position > oldPosition)
				.OrderBy(d => d.Position)
				.ToArrayAsync();

			foreach (var s in siblings)
				s.Position--;

			int newPosition = await dbContext.FragmentLinks
				.CountAsync(l => l.ContainerRef == linkId && l.DocumentRef == docId);


			link.Position = newPosition;
			link.ContainerRef = linkId;

			doc.ModifiedAt = DateTimeOffset.UtcNow;
			doc.Author = user.Identity.Name;

			await dbContext.SaveChangesAsync();

			await _notifier.Notify("on_doc_update", doc.RootSlug, doc.Path, doc.Id);

			return Result<DtoDocumentChangeResult>.Success(new() { Author = doc.Author, ModifiedAt = doc.ModifiedAt });
		}

		public static IReadOnlyList<DtoFragmentElement> NewFragmentElementValue(string path, string lang, IDictionary<string, XSElement> index)
		{
			if (!index.TryGetValue(path, out XSElement xse))
				return null;

			int level = path.Count(c => c == '\\');

			List<DtoFragmentElement> result = [
				new(xse, lang) { Level = level, Path = path, Value = xse.DefaultValue }
			];

			if (!xse.IsSimple)
				for (int i = 0; i < xse.Elements.Count; i++)
					AddDefaultValue(xse.Elements[i], level + 1, lang, result);

			return result;
		}

	}

}