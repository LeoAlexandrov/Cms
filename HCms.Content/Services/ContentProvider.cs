using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using AleProjects.Hashing.MurmurHash3;
using Entities = HCms.Domain.Entities;
using HCms.Domain.ValueObjects;
using HCms.Content.ViewModels;
using HCms.Infrastructure.Data;


namespace HCms.Content.Services
{
	public partial class ContentProvidingService(CmsDbContext dbContext, FragmentSchemaRepo fsr, ILogger<ContentProvidingService> logger)
	{
		[GeneratedRegex("\\^\\(\\d+\\)")]
		private static partial Regex RefRegex();

		[GeneratedRegex("\\^\\('[a-zA-Z0-9+/%]+'\\)")]
		private static partial Regex MediaLinkRegex();


		readonly CmsDbContext _dbContext = dbContext;
		readonly FragmentSchemaRepo _fsr = fsr;
		readonly ILogger<ContentProvidingService> _logger = logger;


		struct Reference(string pattern, string docPath, string mediaLink, string root, IPathMapper pathMapper)
		{
			public string Pattern { get; set; } = pattern;
			public string Replacement { get; set; } = string.IsNullOrEmpty(mediaLink) ?
					pathMapper.Map(root, docPath, false) :
					pathMapper.Map(root, mediaLink, true);
		}


		#region internal-functions

		class MatchComparer : IComparer<Match>
		{
			public int Compare(Match x, Match y)
			{
				return x.Index.CompareTo(y.Index);
			}
		}

		static string GetDomId(string name)
		{
			if (string.IsNullOrEmpty(name))
				return null;

			int n = name.Length;
			Span<char> cId = stackalloc char[n];

			for (int i = 0; i < n; i++)
				if (name[i] == '-' || name[i] == '_' || char.IsLetterOrDigit(name[i]))
					cId[i] = name[i];
				else
					cId[i] = '-';

			return new string(cId);
		}

		static object Convert(string val, int type, IFormatProvider fmt)
		{
			object result;

			switch (type)
			{
				case 1:
					if (int.TryParse(val, out int iRes))
						result = iRes;
					else if (long.TryParse(val, out long lRes))
						result = lRes;
					else
						result = val;
					break;

				case 2:
					if (double.TryParse(val, fmt, out double xRes))
						result = xRes;
					else
						result = val;
					break;

				case 3:
					result = string.Compare(val, "true", true) == 0;
					break;

				default:
					result = val;
					break;
			}

			return result;
		}

		static IEnumerable<string> GetXmlKeys(XElement node, string ns)
		{
			if (node == null)
				return [];

			var all = node.Elements();

			if (string.IsNullOrEmpty(ns))
				return all.Select(e => e.Name.ToString()).Distinct();

			string prefix = $"{{{ns}}}";

			return all.Select(e => e.Name.ToString())
				.Where(name => name.StartsWith(prefix))
				.Select(name => name[prefix.Length..])
				.Distinct();
		}

		static object GetXmlMember(XElement node, string ns, XSElement xse, string memberName, IFormatProvider fmt, out bool success)
		{
			string name = string.IsNullOrEmpty(ns) ? memberName : $"{{{ns}}}{memberName}";
			var nodes = node.Elements(name);
			var any = nodes.Any();

			if (!any && memberName.Contains('_'))
			{
				memberName = memberName.Replace('_', '-');
				name = string.IsNullOrEmpty(ns) ? memberName : $"{{{ns}}}{memberName}";
				nodes = node.Elements(name);
				any = nodes.Any();
			}

			if (!any)
			{
				success = false;
				return null;
			}

			XSElement newXse = null;
			int mtype = -1;
			bool isArray = false;

			if (xse != null)
			{
				for (int i = 0; i < xse.Elements.Count; i++)
					if (xse.Elements[i].Name == memberName)
					{
						newXse = xse.Elements[i];
						isArray = newXse.MaxOccurs > 1;

						if (newXse.IsSimple)
							mtype = newXse.XmlType switch
							{
								"int" or "integer" or "short" or "byte" => 1,
								"double" or "decimal" or "float" => 2,
								"boolean" or "bool" => 3,
								_ => 0, // string
							};

						break;
					}
			}

			Dictionary<string, object> getObject(XElement n)
			{
				var dict = new Dictionary<string, object>();
				var keys = GetXmlKeys(n, ns);

				foreach (var key in keys)
					dict[key] = GetXmlMember(n, ns, newXse, key, fmt, out _);

				return dict;
			}

			object result;

			success = true;

			if (isArray)
			{
				result = nodes.Select(n => n.HasElements ? getObject(n) : Convert(n.Value, mtype, fmt)).ToArray();
				return result;
			}

			var n = nodes.FirstOrDefault();

			if (n.HasElements ||
				n.HasAttributes ||
				(n.FirstNode != null && n.FirstNode.NodeType == System.Xml.XmlNodeType.Comment))
			{
				result = getObject(n);
			}
			else
			{
				result = Convert(n.Value, mtype, fmt);
			}

			return result;
		}

		Dictionary<string, object> PropertiesFromXml(string xml, IList<XSElement> xs)
		{
			XElement root;

			try
			{
				root = XDocument.Parse(xml).Root;
			}
			catch (Exception ex)
			{
				_logger?.LogError(ex, "Error in XDocument.Parse");
				return [];
			}

			IFormatProvider fmt = new NumberFormatInfo();
			XSElement xse = null;
			string ns = null;
			XAttribute attr;

			if ((attr = root?.Attribute("xmlns")) != null)
			{
				ns = attr.Value;

				string name = root.Name.LocalName;
				xse = xs.FirstOrDefault(e => e.Name == name && e.Namespace == ns);
			}

			var result = new Dictionary<string, object>();
			var keys = GetXmlKeys(root, ns);

			foreach (var key in keys)
			{
				var val = GetXmlMember(root, ns, xse, key, fmt, out bool success);

				if (success)
					result[key] = val;
				else
					result[key] = null;
			}

			return result;
		}

		Fragment FragmentFromLink(Entities.FragmentLink link, IEnumerable<Entities.FragmentAttribute> attrs, IList<XSElement> xse)
		{
			return new Fragment()
			{
				Id = link.FragmentRef,
				LinkId = link.Id,
				Container = link.Parent,
				Name = link.Title,
				Icon = link.Icon,
				Shared = link.Fragment.Shared,
				Status = link.Status,
				Anchor = link.Anchor,
				XmlName = link.Fragment.XmlName,
				XmlSchema = link.Fragment.XmlSchema,
				Props2 = PropertiesFromXml(link.Fragment.Data, xse),
				Attributes = attrs.ToDictionary(a => a.AttributeKey, a => a.Value)
			};
		}

		static Document DocumentFromEntity(Entities.Document doc, string url, BreadcrumbsItem[] breadcrumbs = null)
		{
			return new Document()
			{
				Id = doc.Id,
				ParentId = doc.Parent,
				Position = doc.Position,
				Slug = doc.Slug,
				Path = doc.Path,
				Root = doc.RootSlug,
				Title = doc.Title,
				Summary = doc.Summary,
				CoverPicture = doc.CoverPicture,
				Language = doc.Language,
				Icon = doc.Icon,
				Url = url,
				Tags = doc.Tags?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
				AuthPolicies = doc.AuthPolicies,
				Status = doc.Status,
				CreatedAt = doc.CreatedAt.UtcDateTime,
				ModifiedAt = doc.ModifiedAt.UtcDateTime,
				Author = doc.Author,
				Children = [],
				Siblings = [],
				Breadcrumbs = breadcrumbs
			};
		}

		static void SetChildren(Fragment fr, Dictionary<int, Memory<Entities.FragmentLink>> tree, Func<Entities.FragmentLink, Fragment> fragmentFactory)
		{
			if (tree.TryGetValue(fr.LinkId, out Memory<Entities.FragmentLink> children))
			{
				Span<Entities.FragmentLink> span = children.Span;
				Fragment[] frChildren = new Fragment[span.Length];

				for (int i = 0; i < span.Length; i++)
					SetChildren(frChildren[i] = fragmentFactory(span[i]), tree, fragmentFactory);

				fr.Children = frChildren;
			}
		}

		Fragment[] CreateFragmentsTree(Entities.FragmentLink[] links, ILookup<int, Entities.FragmentAttribute> fragmentAttrs, IList<XSElement> xse)
		{
			Fragment fragmentFactory(Entities.FragmentLink link) => FragmentFromLink(link, fragmentAttrs[link.FragmentRef], xse);

			Dictionary<int, Memory<Entities.FragmentLink>> tree = [];

			if (links.Length > 0)
			{
				int key = links[0].Parent;
				int from = 0;

				for (int i = 0; i < links.Length; i++)
					if (!key.Equals(links[i].Parent))
					{
						tree.Add(key, links.AsMemory(from, i - from));
						from = i;
						key = links[i].Parent;
					}

				tree.Add(key, links.AsMemory(from, links.Length - from));
			}

			Fragment[] result;

			if (tree.TryGetValue(default, out Memory<Entities.FragmentLink> roots))
			{
				result = roots
					.ToArray()
					.Select(fl => FragmentFromLink(fl, fragmentAttrs[fl.FragmentRef], xse))
					.ToArray();

				foreach (var d in result)
					SetChildren(d, tree, fragmentFactory);
			}
			else
			{
				result = [];
			}

			return result;
		}

		static void FillAnchors(Entities.FragmentLink[] links, int linkIdx, List<Anchor> anchors, int level)
		{
			int cref = links[linkIdx].ContainerRef;

			do
			{
				if (links[linkIdx].Anchor)
				{
					string name = links[linkIdx].Fragment?.Name;

					anchors.Add(new Anchor() { Id = GetDomId(name), Name = name, Level = level });

					int i = Array.FindIndex(links, linkIdx, l => l.ContainerRef == links[linkIdx].Id && l.Anchor);

					if (i >= 0)
						FillAnchors(links, i, anchors, level + 1);
				}

				linkIdx++;
			} while (linkIdx < links.Length && links[linkIdx].ContainerRef == cref);
		}

		static string ReplaceRefs(string content, Dictionary<string, string> refs)
		{
			if (string.IsNullOrEmpty(content))
				return string.Empty;

			var re1 = RefRegex();
			MatchCollection matchesD = re1.Matches(content);

			var re2 = MediaLinkRegex();
			MatchCollection matchesM = re2.Matches(content);

			if (matchesD.Count + matchesM.Count == 0)
				return content;

			var matches = new Match[matchesD.Count + matchesM.Count];

			matchesD.CopyTo(matches, 0);
			matchesM.CopyTo(matches, matchesD.Count);

			Array.Sort(matches, new MatchComparer());

			StringBuilder result = new();
			var span = content.AsSpan();
			int from = 0;

			foreach (Match m in matches)
			{
				result.Append(span[from..m.Index]);

				if (refs.TryGetValue(m.Value, out string link))
					result.Append(link);
				else
					result.Append(m.Value);

				from = m.Index + m.Length;
			}

			if (from < content.Length)
				result.Append(span[from..content.Length]);

			return result.ToString();
		}

		#endregion

		public async Task<Document> GetDocument(IPathMapper pathMapper, string root, string path, int childrenFromPos, int takeChildren, bool siblings, int[] allowedStatus, bool exactPathMatch)
		{
			allowedStatus ??= [(int)PublishStatus.Published];

			var query = _dbContext.Documents
				.AsNoTracking()
				.Where(d => d.Parent == 0 && allowedStatus.Contains(d.Status));

			if (!string.IsNullOrEmpty(root))
				query = query.Where(d => d.Slug == root);

			var rootDoc = await query.FirstOrDefaultAsync();

			if (rootDoc == null)
				return null;


			int rootId = rootDoc.Id;
			string rootKey = rootDoc.Slug;
			Document result;
			Entities.Document doc;
			List<int> allDocsIds;

			if (string.IsNullOrEmpty(path) || path == "/")
			{
				doc = rootDoc;
				allDocsIds = [rootId];

				result = DocumentFromEntity(doc, 
					pathMapper.Map(rootKey, "/"), 
					[new BreadcrumbsItem() { Path = pathMapper.Map(rootKey, "/"), Title = string.Empty, Document = rootId }]
				);
			}
			else
			{
				if (path.Contains("//") || path[0] != '/')
					throw new ArgumentException("Invalid format.", nameof(path));

				string pathNoSlash = path[^1] == '/' ? path[..^1] : path;
				int k = pathNoSlash.Length;
				int n = pathNoSlash.Count(c => c == '/');

				long[] hashes = new long[n];
				string[] pathes = new string[n];

				for (int i = 0; i < n; i++)
				{
					pathes[i] = pathNoSlash[..k];
					hashes[i] = MurmurHash3.Hash32(pathes[i]);
					k = pathNoSlash.LastIndexOf('/', k - 1);
				}

				var docs = await _dbContext.Documents
					.AsNoTracking()
					.Join(_dbContext.DocumentPathNodes, d => d.Id, n => n.DocumentRef, (d, n) => new { d, n })
					.Where(dn => dn.n.Parent == rootId && hashes.Contains(dn.d.PathHash) && allowedStatus.Contains(dn.d.Status))
					.Select(dn => dn.d)
					.ToListAsync();


				docs.RemoveAll(d => !pathes.Contains(d.Path)); // if hash collisions
				docs.Sort((d1, d2) => string.Compare(d1.Path, d2.Path, StringComparison.InvariantCultureIgnoreCase));
				doc = docs.Count != 0 ? docs[^1] : rootDoc;

				bool exact = docs.Count == n;

				if (exactPathMatch && !exact)
					return null;

				n = docs.Count;


				var breadcrumbs = new BreadcrumbsItem[n + 1];
				breadcrumbs[0] = new() { Path = pathMapper.Map(rootKey, "/"), Title = string.Empty, Document = rootId };

				for (int i = 0; i < n; i++)
					breadcrumbs[i + 1] = new() { Path = pathMapper.Map(rootKey, docs[i].Path), Title = docs[i].Title, Document = docs[i].Id };

				allDocsIds = [.. breadcrumbs.Select(b => b.Document)];

				result = DocumentFromEntity(doc, pathMapper.Map(rootKey, doc.Path), breadcrumbs);
				result.ExactMatch = exact;

				if (siblings)
				{
					var q = Children(doc.Parent, -1, -1, allowedStatus);
					result.Siblings = await q.Select(d => DocumentFromEntity(d, pathMapper.Map(d.RootSlug, d.Path, false), null)).ToArrayAsync();
				}
			}


			var attrs = await _dbContext.DocumentAttributes
				.AsNoTracking()
				.Where(a => allDocsIds.Contains(a.DocumentRef) && a.Enabled)
				.OrderBy(a => a.DocumentRef)
				.ToArrayAsync();

			result.Attributes = [];

			int aIdx;

			foreach (int docId in allDocsIds)
				if ((aIdx = Array.FindIndex(attrs, a => a.DocumentRef == docId)) >= 0)
				{
					while (aIdx < attrs.Length)
					{
						if (attrs[aIdx].DocumentRef == doc.Id || !attrs[aIdx].Private)
							result.Attributes[attrs[aIdx].AttributeKey] = attrs[aIdx].Value;

						aIdx++;
					}
				}

			if (childrenFromPos >= 0)
			{
				result.ChildrenTakePosition = childrenFromPos;
				result.ChildrenTaken = takeChildren;
				result.TotalChildrenCount = await _dbContext.Documents.Where(d => d.Parent == doc.Id).CountAsync();

				var q = Children(doc.Id, childrenFromPos, takeChildren, allowedStatus);
				result.Children = await q.Select(d => DocumentFromEntity(d, pathMapper.Map(d.RootSlug, d.Path, false), null)).ToArrayAsync();
				
				allDocsIds.AddRange(result.Children.Select(d => d.Id));
			}

			allDocsIds.AddRange(result.Siblings.Where(d => d.Id != doc.Id).Select(d => d.Id));


			var refsList = await _dbContext.References
				.AsNoTracking()
				.GroupJoin(_dbContext.Documents, r => r.ReferenceTo, d => d.Id, (r, d) => new { r, d })
				.Where(rd => allDocsIds.Contains(rd.r.DocumentRef))
				.SelectMany(
					rd => rd.d.DefaultIfEmpty(),
					(r, d) => new Reference(r.r.Encoded, d.Path, r.r.MediaLink, d.RootSlug, pathMapper)
				)
				.ToArrayAsync();

			Dictionary<string, string> refs = [];

			foreach (var r in refsList)
				refs.TryAdd(r.Pattern, r.Replacement);


			result.Summary = ReplaceRefs(result.Summary, refs);
			result.CoverPicture = ReplaceRefs(result.CoverPicture, refs);

			Entities.FragmentLink[] links = await _dbContext.FragmentLinks
				.AsNoTracking()
				.Include(b => b.Fragment)
				.Where(b => b.DocumentRef == doc.Id && allowedStatus.Contains(b.Status))
				.OrderBy(b => b.ContainerRef)
				.ThenBy(b => b.Position)
				.ToArrayAsync();

			int anchorsCount = links.Count(l => l.Anchor);

			if (anchorsCount > 0)
			{
				result.Anchors = new List<Anchor>(anchorsCount);

				FillAnchors(links, 0, result.Anchors, 0);
			}

			foreach (var link in links)
				link.Fragment.Data = ReplaceRefs(link.Fragment.Data, refs);

			foreach (var key in result.Attributes.Keys)
				result.Attributes[key] = ReplaceRefs(result.Attributes[key], refs);


			foreach (var s in result.Siblings)
			{
				s.Summary = ReplaceRefs(s.Summary, refs);
				s.CoverPicture = ReplaceRefs(s.CoverPicture, refs);
			}

			foreach (var c in result.Children)
			{
				c.Summary = ReplaceRefs(c.Summary, refs);
				c.CoverPicture = ReplaceRefs(c.CoverPicture, refs);
			}


			var fAttrs = await _dbContext.FragmentLinks
				.AsNoTracking()
				.Join(_dbContext.Fragments, l => l.FragmentRef, f => f.Id, (l, f) => new { l, f })
				.Where(lf => lf.l.DocumentRef == doc.Id && lf.l.Status != (int)PublishStatus.Unpublished)
				.Join(_dbContext.FragmentAttributes, lf => lf.f.Id, a => a.FragmentRef, (lf, a) => a)
				.Where(a => a.Enabled)
				.ToArrayAsync();

			foreach (var f in fAttrs)
				f.Value = ReplaceRefs(f.Value, refs);

			result.Fragments = CreateFragmentsTree(links, fAttrs.ToLookup(a => a.FragmentRef, a => a), _fsr.Fragments);

			return result;
		}

		public async Task<Document> GetDocument(IPathMapper pathMapper, int id, int childrenFromPos, int takeChildren, bool siblings, int[] allowedStatus)
		{
			allowedStatus ??= [(int)PublishStatus.Published];

			var doc = await _dbContext.Documents
				.AsNoTracking()
				.FirstOrDefaultAsync(d => d.Id == id && allowedStatus.Contains(d.Status));

			if (doc == null)
				return null;


			string rootKey;
			Document result;
			List<int> allDocsIds;

			if (doc.Parent <= 0)
			{
				rootKey = doc.Slug;
				allDocsIds = [id];

				result = DocumentFromEntity(doc, 
					pathMapper.Map(rootKey, "/"), 
					[new BreadcrumbsItem() { Path = pathMapper.Map(rootKey, "/"), Title = string.Empty, Document = id }]
				);
			}
			else
			{
				var docs = await _dbContext.Documents
					.AsNoTracking()
					.Join(_dbContext.DocumentPathNodes, d => d.Id, n => n.Parent, (d, n) => new { d, n })
					.Where(dn => dn.n.DocumentRef == id)
					.Select(dn => dn.d)
					.OrderBy(d => d.Id)
					.ToListAsync();

				docs.Sort((d1, d2) => string.Compare(d1.Path, d2.Path, StringComparison.InvariantCultureIgnoreCase));

				rootKey = docs[0].Slug;

				var breadcrumbs = new BreadcrumbsItem[docs.Count + 1];
				breadcrumbs[0] = new BreadcrumbsItem() { Path = pathMapper.Map(rootKey, "/"), Title = string.Empty, Document = docs[0].Id };
				breadcrumbs[^1] = new BreadcrumbsItem() { Path = pathMapper.Map(rootKey, doc.Path), Title = doc.Title, Document = id };

				for (int i = 1; i < docs.Count; i++)
					breadcrumbs[i] = new BreadcrumbsItem() { Path = pathMapper.Map(rootKey, docs[i].Path), Title = docs[i].Title, Document = docs[i].Id };

				result = DocumentFromEntity(doc, pathMapper.Map(rootKey, doc.Path), breadcrumbs);
				allDocsIds = [.. docs.Select(d => d.Id), doc.Id];

				if (siblings)
				{
					var q = Children(doc.Parent, -1, -1, allowedStatus);
					result.Siblings = await q.Select(d => DocumentFromEntity(d, pathMapper.Map(d.RootSlug, d.Path, false), null)).ToArrayAsync();
				}
			}


			var attrs = await _dbContext.DocumentAttributes
				.AsNoTracking()
				.Where(a => allDocsIds.Contains(a.DocumentRef) && a.Enabled)
				.OrderBy(a => a.DocumentRef)
				.ToArrayAsync();

			result.Attributes = [];

			int aIdx;

			foreach (int docId in allDocsIds)
				if ((aIdx = Array.FindIndex(attrs, a => a.DocumentRef == docId)) >= 0)
				{
					while (aIdx < attrs.Length)
					{
						if (attrs[aIdx].DocumentRef == id || !attrs[aIdx].Private)
							result.Attributes[attrs[aIdx].AttributeKey] = attrs[aIdx].Value;

						aIdx++;
					}
				}


			if (childrenFromPos >= 0)
			{
				result.ChildrenTakePosition = childrenFromPos;
				result.ChildrenTaken = takeChildren;
				result.TotalChildrenCount = await _dbContext.Documents.Where(d => d.Parent == id).CountAsync();

				var q = Children(doc.Id, childrenFromPos, takeChildren, allowedStatus);
				result.Children = await q.Select(d => DocumentFromEntity(d, pathMapper.Map(d.RootSlug, d.Path, false), null)).ToArrayAsync();

				allDocsIds.AddRange(result.Children.Select(d => d.Id));
			}
			else
			{
				result.Children = [];
			}

			allDocsIds.AddRange(result.Siblings.Where(d => d.Id != doc.Id).Select(d => d.Id));


			var refsList = await _dbContext.References
				.AsNoTracking()
				.GroupJoin(_dbContext.Documents, r => r.ReferenceTo, d => d.Id, (r, d) => new { r, d })
				.Where(rd => allDocsIds.Contains(rd.r.DocumentRef))
				.SelectMany(
					rd => rd.d.DefaultIfEmpty(),
					(r, d) => new Reference(r.r.Encoded, d.Path, r.r.MediaLink, d.RootSlug, pathMapper)
				)
				.ToArrayAsync();

			Dictionary<string, string> refs = [];

			foreach (var r in refsList)
				refs.TryAdd(r.Pattern, r.Replacement);


			result.Summary = ReplaceRefs(result.Summary, refs);
			result.CoverPicture = ReplaceRefs(result.CoverPicture, refs);

			Entities.FragmentLink[] links = await _dbContext.FragmentLinks
				.AsNoTracking()
				.Include(b => b.Fragment)
				.Where(b => b.DocumentRef == id && allowedStatus.Contains(b.Status))
				.OrderBy(b => b.ContainerRef)
				.ThenBy(b => b.Position)
				.ToArrayAsync();

			int anchorsCount = links.Count(l => l.Anchor);

			if (anchorsCount > 0)
			{
				result.Anchors = new List<Anchor>(anchorsCount);

				FillAnchors(links, 0, result.Anchors, 0);
			}

			foreach (var link in links)
				link.Fragment.Data = ReplaceRefs(link.Fragment.Data, refs);

			foreach (var key in result.Attributes.Keys)
				result.Attributes[key] = ReplaceRefs(result.Attributes[key], refs);

			foreach (var s in result.Siblings)
			{
				s.Summary = ReplaceRefs(s.Summary, refs);
				s.CoverPicture = ReplaceRefs(s.CoverPicture, refs);
			}

			foreach (var c in result.Children)
			{
				c.Summary = ReplaceRefs(c.Summary, refs);
				c.CoverPicture = ReplaceRefs(c.CoverPicture, refs);
			}


			var fAttrs = await _dbContext.FragmentLinks
				.AsNoTracking()
				.Join(_dbContext.Fragments, l => l.FragmentRef, f => f.Id, (l, f) => new { l, f })
				.Where(lf => lf.l.DocumentRef == id && lf.l.Status != (int)PublishStatus.Unpublished)
				.Join(_dbContext.FragmentAttributes, lf => lf.f.Id, a => a.FragmentRef, (lf, a) => a)
				.Where(a => a.Enabled)
				.ToArrayAsync();

			foreach (var f in fAttrs)
				f.Value = ReplaceRefs(f.Value, refs);

			result.Fragments = CreateFragmentsTree(links, fAttrs.ToLookup(a => a.FragmentRef, a => a), _fsr.Fragments);

			return result;
		}

		IQueryable<Entities.Document> Children(int id, int childrenFromPos, int take, int[] allowedStatus)
		{
			if (take < 0)
				take = 1000;

			IQueryable<Entities.Document> query;

			if (allowedStatus != null)
				query = _dbContext.Documents
					.AsNoTracking()
					.Where(d => d.Parent == id && allowedStatus.Contains(d.Status) && d.Position >= childrenFromPos)
					.OrderBy(d => d.Position)
					.Take(take);
			else
				query = _dbContext.Documents
					.AsNoTracking()
					.Where(d => d.Parent == id && d.Status == (int)PublishStatus.Published && d.Position >= childrenFromPos)
					.OrderBy(d => d.Position)
					.Take(take);

			return query;
		}

		public async Task<Document[]> GetChildren(IPathMapper pathMapper, int id, int childrenFromPos, int take, int[] allowedStatus)
		{
			var query = Children(id, childrenFromPos, take, allowedStatus);
			var result = await query.Select(d => DocumentFromEntity(d, pathMapper.Map(d.RootSlug, d.Path, false), null)).ToArrayAsync();
			var allDocsIds = result.Select(d => d.Id).ToArray();

			var refsList = await _dbContext.References
				.AsNoTracking()
				.GroupJoin(_dbContext.Documents, r => r.ReferenceTo, d => d.Id, (r, d) => new { r, d })
				.Where(rd => allDocsIds.Contains(rd.r.DocumentRef))
				.SelectMany(
					rd => rd.d.DefaultIfEmpty(),
					(r, d) => new Reference(r.r.Encoded, d.Path, r.r.MediaLink, d.RootSlug, pathMapper)
				)
				.ToArrayAsync();

			Dictionary<string, string> refs = [];

			foreach (var r in refsList)
				refs.TryAdd(r.Pattern, r.Replacement);

			foreach (var doc in result)
			{
				doc.Summary = ReplaceRefs(doc.Summary, refs);
				doc.CoverPicture = ReplaceRefs(doc.CoverPicture, refs);
			}

			return result;
		}

		public async Task<(string, string)> IdToPath(int id)
		{
			var doc = await _dbContext.Documents
				.AsNoTracking()
				.FirstOrDefaultAsync(d => d.Id == id);

			if (doc == null)
				return (null, null);

			if (doc.Parent == 0)
				return (doc.Slug, doc.Path);

			var root = await _dbContext.Documents
				.AsNoTracking()
				.Join(_dbContext.DocumentPathNodes, d => d.Id, n => n.Parent, (d, n) => new { d, n })
				.Where(dn => dn.n.DocumentRef == id && dn.d.Parent == 0)
				.Select(dn => dn.d)
				.OrderBy(d => d.Id)
				.FirstOrDefaultAsync();

			return (root.Slug, doc.Path);
		}

		public async ValueTask<string> UserRole(string login)
		{
			var user = await _dbContext.Users
				.AsNoTracking()
				.FirstOrDefaultAsync(u => u.Login == login);

			return user?.Role;
		}

	}
}