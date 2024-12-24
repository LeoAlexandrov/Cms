using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using AleProjects.Base64;
using Entities = AleProjects.Cms.Domain.Entities;
using AleProjects.Cms.Domain.ValueObjects;
using AleProjects.Cms.Infrastructure.Data;
using AleProjects.Cms.Sdk.ViewModels;
using System.Threading;


namespace AleProjects.Cms.Sdk.ContentRepo
{
	public delegate string ReferenceTransformer(string documentPath, string mediaPath);


	public partial class ContentRepo : IDisposable
	{
		readonly CmsDbContext dbContext;
		readonly string mediaHost;
		readonly static FragmentSchemaRepo fsr = new();
		readonly static object lockObject = new();
		static int NeedsSchemataReload = 1;
		static int PageSize = 25;
		bool disposed;

		[GeneratedRegex("#\\(\\d+\\)")]
		private static partial Regex RefRegex();

		[GeneratedRegex("#\\('[a-zA-Z0-9+/%]+'\\)")]
		private static partial Regex MediaLinkRegex();


		class MatchComparer : IComparer<Match>
		{
			public int Compare(Match x, Match y)
			{
				return x.Index.CompareTo(y.Index);
			}
		}

		struct Reference
		{
			public string Pattern { get; set; }
			public string Replacement { get; set; }
		}


		public ContentRepo(IConfiguration configuration)
		{
			string connString = configuration.GetConnectionString("CmsDbConnection");
			var contextOptions = new DbContextOptionsBuilder<CmsDbContext>().UseSqlServer(connString).Options;
			dbContext = new CmsDbContext(contextOptions);

			mediaHost = configuration["MediaHost"];

			if (!mediaHost.EndsWith('/'))
				mediaHost += "/";

			LoadFragmentSchemaService(dbContext);
		}

		#region private-functions

		private static void LoadFragmentSchemaService(CmsDbContext dbContext)
		{
			if (NeedsSchemataReload > 0)
				lock (lockObject)
				{
					if (NeedsSchemataReload > 0)
					{
						fsr.Reload(dbContext);
						NeedsSchemataReload = 0;
					}
				}
		}

		private static void SetChildren(Fragment fr, Dictionary<int, Memory<Entities.FragmentLink>> tree, Func<Entities.FragmentLink, Fragment> fragmentFactory)
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

		private static Fragment[] CreateFragmentsTree(Entities.FragmentLink[] links, Document doc, ILookup<int, Entities.FragmentAttribute> fragmentAttrs, IList<XSElement> xse)
		{
			Fragment fragmentFactory(Entities.FragmentLink link) => Fragment.Create(link, doc, fragmentAttrs[link.FragmentRef], xse);

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
					.Select(fl => Fragment.Create(fl, doc, fragmentAttrs[fl.FragmentRef], xse))
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

		private static void FillAnchors(Entities.FragmentLink[] links, int linkIdx, List<Anchor> anchors, int level)
		{
			int cref = links[linkIdx].ContainerRef;

			do
			{
				if (links[linkIdx].Anchor)
				{
					anchors.Add(new Anchor() { Id = links[linkIdx].Fragment.XmlName + links[linkIdx].Id, Name = links[linkIdx].Fragment?.Name, Level = level });

					int i = Array.FindIndex(links, linkIdx, l => l.ContainerRef == links[linkIdx].Id && l.Anchor);

					if (i >= 0)
						FillAnchors(links, i, anchors, level + 1);
				}

				linkIdx++;
			} while (linkIdx < links.Length && links[linkIdx].ContainerRef == cref);
		}

		private static string ReplaceRefs(string content, Dictionary<string, string> refs)
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

		private string DefaultRefTransformer(string documentPath, string mediaPath)
		{
			if (string.IsNullOrEmpty(mediaPath))
				return documentPath;

			if (string.IsNullOrEmpty(mediaHost))
				return mediaPath;

			return mediaHost + mediaPath;
		}

		#endregion


		public static void ReloadSchemata()
		{
			NeedsSchemataReload = 1;
		}

		public async Task<Document> GetDocument(string root, string path, int childrenFromPos, bool siblings, ReferenceTransformer refTransformer)
		{
			var rootDoc = await dbContext.Documents
				.AsNoTracking()
				.Where(d => d.Parent == 0 && d.Slug == root && d.Published)
				.FirstOrDefaultAsync();

			if (rootDoc == null)
				return null;

			int rootId = rootDoc.Id;

			Document result;
			BreadcrumbsItem[] breadcrumbs;
			Entities.Document doc;
			List<int> allDocsIds;

			if (string.IsNullOrEmpty(path) || path == "/")
			{
				doc = rootDoc;
				breadcrumbs = [new BreadcrumbsItem() { Path = "/", Title = string.Empty }];
				allDocsIds = [rootId];

				result = new(doc)
				{
					Breadcrumbs = breadcrumbs,
					Siblings = []
				};
			}
			else
			{
				string[] slugs = path.ToLower().Split('/', StringSplitOptions.RemoveEmptyEntries);

				var docs = await dbContext.Documents
					.AsNoTracking()
					.Join(dbContext.DocumentPathNodes, d => d.Id, n => n.DocumentRef, (d, n) => new { d, n })
					.Where(dn => dn.n.Parent == rootId && slugs.Contains(dn.d.Slug) && dn.d.Published)
					.Select(dn => dn.d)
					.OrderBy(d => d.Id)
					.ToArrayAsync();

				string pathNoSlash = path[^1] == '/' ? path[..^1] : path;

				doc = docs.FirstOrDefault(d => string.Compare(d.Path, pathNoSlash, StringComparison.InvariantCultureIgnoreCase) == 0);

				if (doc == null)
					return null;

				Document parent = slugs.Length < 2 ? new Document(rootDoc) : null;
				int[] ids = docs.Select(d => d.Id).ToArray();
				int id = doc.Parent;

				breadcrumbs = new BreadcrumbsItem[slugs.Length + 1];

				breadcrumbs[0] = new BreadcrumbsItem() { Path = "/", Title = string.Empty };
				breadcrumbs[^1] = new BreadcrumbsItem() { Path = doc.Path, Title = doc.Title };


				for (int i = 1; i < slugs.Length; i++)
				{
					int j = Array.BinarySearch(ids, id);

					id = docs[j].Parent;
					breadcrumbs[^(i + 1)] = new BreadcrumbsItem() { Path = docs[j].Path, Title = docs[j].Title };

					if (i == 1)
						parent = new Document(docs[j]);
				}

				allDocsIds = [doc.Id, parent.Id];

				result = new(doc)
				{
					Parent = parent,
					Breadcrumbs = breadcrumbs
				};

				if (siblings)
				{
					result.Siblings = await Children(doc.Parent, -1);
					allDocsIds.AddRange(result.Siblings.Where(d => d.Id != doc.Id).Select(d => d.Id));
				}
				else
					result.Siblings = [];
			}


			result.Attributes = await dbContext.DocumentAttributes
				.AsNoTracking()
				.Where(a => a.DocumentRef == doc.Id && a.Enabled)
				.OrderBy(a => a.AttributeKey)
				.ToDictionaryAsync(a => a.AttributeKey, a => a.Value);


			if (childrenFromPos >= 0)
			{
				result.ChildrenPosition = childrenFromPos;
				result.TotalChildCount = await dbContext.Documents.Where(d => d.Parent == doc.Id).CountAsync();
				result.Children = await Children(doc.Id, childrenFromPos);
				allDocsIds.AddRange(result.Children.Select(d => d.Id));
			}
			else
				result.Children = [];

			refTransformer ??= DefaultRefTransformer;

			var refs = await dbContext.References
				.AsNoTracking()
				.GroupJoin(dbContext.Documents, r => r.ReferenceTo, d => d.Id, (r, d) => new { r, d })
				.Where(rd => allDocsIds.Contains(rd.r.DocumentRef))
				.SelectMany(
					rd => rd.d.DefaultIfEmpty(),
					(r, d) => new Reference() { Pattern = r.r.Encoded, Replacement = refTransformer(d.Path, r.r.MediaLink) }
				)
				.ToDictionaryAsync(r => r.Pattern, r => r.Replacement);

			result.Summary = ReplaceRefs(result.Summary, refs);
			result.CoverPicture = ReplaceRefs(result.CoverPicture, refs);

			Entities.FragmentLink[] links = await dbContext.FragmentLinks
				.AsNoTracking()
				.Include(b => b.Fragment)
				.Where(b => b.DocumentRef == doc.Id && b.Enabled)
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

			var attrs = await dbContext.FragmentLinks
				.AsNoTracking()
				.Join(dbContext.Fragments, l => l.FragmentRef, f => f.Id, (l, f) => new { l, f })
				.Where(lf => lf.l.DocumentRef == doc.Id && lf.l.Enabled)
				.Join(dbContext.FragmentAttributes, lf => lf.f.Id, a => a.FragmentRef, (lf, a) => a)
				.Where(a => a.Enabled)
				.ToArrayAsync();

			result.Fragments = CreateFragmentsTree(links, result, attrs.ToLookup(a => a.FragmentRef, a => a), fsr.Fragments);

			return result;
		}

		public Task<Document[]> Children(int docId, int childrenFromPos)
		{
			var qry = dbContext.Documents
				.AsNoTracking()
				.Where(d => d.Parent == docId && d.Published && d.Position >= childrenFromPos)
				.OrderBy(d => d.Position);

			if (childrenFromPos > 0)
				return qry.Take(PageSize).Select(d => new Document(d)).ToArrayAsync();

			return qry.Select(d => new Document(d)).ToArrayAsync();
		}


		#region IDisposable-implementation

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					dbContext.Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null

				disposed = true;
			}
		}

		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~ContentRepo()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
