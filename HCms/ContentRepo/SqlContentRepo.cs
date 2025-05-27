using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Entities = AleProjects.Cms.Domain.Entities;
using AleProjects.Cms.Infrastructure.Data;
using AleProjects.Hashing.MurmurHash3;

using HCms.Routing;
using HCms.ViewModels;


namespace HCms.ContentRepo
{
	/// <summary>
	/// CMS content repository querying database directly. Assumed to be a scoped service.
	/// </summary>
	public class SqlContentRepo : ContentRepo, IContentRepo
	{
		readonly static FragmentSchemaRepo fsr = new();
		readonly static object lockObject = new();
		static int NeedsSchemataReload = 1;

		readonly CmsDbContext dbContext;


		public SqlContentRepo(CmsDbContext dbContext, IPathTransformer pathTransformer)
		{
			this.dbContext = dbContext;
			this.pathTransformer = pathTransformer;

			LoadFragmentSchemaService(dbContext);
		}


		#region private-functions

		static void LoadFragmentSchemaService(CmsDbContext dbContext)
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

		#endregion


		/// <summary>
		/// Forces to reload schemata from the database when the next instance of SqlContentRepo is created.
		/// </summary>
		public void ReloadSchemata()
		{
			NeedsSchemataReload = 1;
		}

		public async Task<Document> GetDocument(string root, string path, int childrenFromPos, int takeChildren, bool siblings, int[] allowedStatus, bool exactPathMatch)
		{
			allowedStatus ??= [(int)Entities.Document.Status.Published];

			var rootDoc = await dbContext.Documents
				.AsNoTracking()
				.FirstOrDefaultAsync(d => d.Parent == 0 && d.Slug == root && allowedStatus.Contains(d.PublishStatus));

			if (rootDoc == null)
				return null;


			int rootId = rootDoc.Id;
			string rootKey = rootDoc.Slug;
			Document result;
			BreadcrumbsItem[] breadcrumbs;
			Entities.Document doc;
			List<int> allDocsIds;

			if (string.IsNullOrEmpty(path) || path == "/")
			{
				doc = rootDoc;
				breadcrumbs = [new BreadcrumbsItem() { Path = pathTransformer.Forward(rootKey, "/", false), Title = string.Empty, Document = rootId }];
				allDocsIds = [rootId];

				result = new(doc)
				{
					Root = new(doc),
					Breadcrumbs = breadcrumbs,
					Siblings = []
				};
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

				var docs = await dbContext.Documents
					.AsNoTracking()
					.Join(dbContext.DocumentPathNodes, d => d.Id, n => n.DocumentRef, (d, n) => new { d, n })
					.Where(dn => dn.n.Parent == rootId && hashes.Contains(dn.d.PathHash) && allowedStatus.Contains(dn.d.PublishStatus))
					.Select(dn => dn.d)
					.ToListAsync();

				
				docs.RemoveAll(d => !pathes.Contains(d.Path)); // if hash collisions

				docs.Sort((d1, d2) => string.Compare(d1.Path, d2.Path, StringComparison.InvariantCultureIgnoreCase));
				doc = docs.Count != 0 ? docs[^1] : rootDoc;

				bool exact = docs.Count == n;

				if (exactPathMatch && !exact)
					return null;

				n = docs.Count;


				breadcrumbs = new BreadcrumbsItem[n + 1];
				breadcrumbs[0] = new() { Path = pathTransformer.Forward(rootKey, "/", false), Title = string.Empty, Document = rootId };

				for (int i = 0; i < n; i++)
				{
					breadcrumbs[i + 1] = new() { Path = pathTransformer.Forward(rootKey, docs[i].Path, false), Title = docs[i].Title, Document = docs[i].Id };
				}

				Document parent = new(n > 1 ? docs[^2] : rootDoc);

				allDocsIds = [.. breadcrumbs.Select(b => b.Document)];

				result = new(doc)
				{
					Parent = parent,
					Root = new(rootDoc),
					Breadcrumbs = breadcrumbs,
					ExactMatch = exact
				};

				if (siblings)
					result.Siblings = await Children(doc.Parent, -1, -1, allowedStatus);
				else
					result.Siblings = [];
			}


		
			var attrs = await dbContext.DocumentAttributes
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
				result.TotalChildrenCount = await dbContext.Documents.Where(d => d.Parent == doc.Id).CountAsync();
				result.Children = await Children(doc.Id, childrenFromPos, takeChildren, allowedStatus);
				allDocsIds.AddRange(result.Children.Select(d => d.Id));
			}
			else
			{
				result.Children = [];
			}

			allDocsIds.AddRange(result.Siblings.Where(d => d.Id != doc.Id).Select(d => d.Id));


			var refsList = await dbContext.References
				.AsNoTracking()
				.GroupJoin(dbContext.Documents, r => r.ReferenceTo, d => d.Id, (r, d) => new { r, d })
				.Where(rd => allDocsIds.Contains(rd.r.DocumentRef))
				.SelectMany(
					rd => rd.d.DefaultIfEmpty(),
					(r, d) => new Reference(r.r.Encoded, d.Path, r.r.MediaLink, d.RootSlug, pathTransformer)
				)
				.ToArrayAsync();

			Dictionary<string, string> refs = [];

			foreach (var r in refsList)
				refs.TryAdd(r.Pattern, r.Replacement);


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


			var fAttrs = await dbContext.FragmentLinks
				.AsNoTracking()
				.Join(dbContext.Fragments, l => l.FragmentRef, f => f.Id, (l, f) => new { l, f })
				.Where(lf => lf.l.DocumentRef == doc.Id && lf.l.Enabled)
				.Join(dbContext.FragmentAttributes, lf => lf.f.Id, a => a.FragmentRef, (lf, a) => a)
				.Where(a => a.Enabled)
				.ToArrayAsync();

			foreach (var f in fAttrs)
				f.Value = ReplaceRefs(f.Value, refs);

			result.Fragments = CreateFragmentsTree(links, result, fAttrs.ToLookup(a => a.FragmentRef, a => a), fsr.Fragments);

			return result;
		}

		public async Task<Document> GetDocument(int id, int childrenFromPos, int takeChildren, bool siblings, int[] allowedStatus)
		{
			allowedStatus ??= [(int)Entities.Document.Status.Published];

			var doc = await dbContext.Documents
				.AsNoTracking()
				.FirstOrDefaultAsync(d => d.Id == id && allowedStatus.Contains(d.PublishStatus));

			if (doc == null)
				return null;


			string rootKey;
			Document result;
			BreadcrumbsItem[] breadcrumbs;
			List<int> allDocsIds;

			if (doc.Parent <= 0)
			{
				rootKey = doc.Slug;
				breadcrumbs = [new BreadcrumbsItem() { Path = pathTransformer.Forward(rootKey, "/", false), Title = string.Empty, Document = id }];
				allDocsIds = [id];

				result = new(doc)
				{
					Root = new(doc),
					Breadcrumbs = breadcrumbs,
					Siblings = []
				};
			}
			else
			{
				var docs = await dbContext.Documents
					.AsNoTracking()
					.Join(dbContext.DocumentPathNodes, d => d.Id, n => n.Parent, (d, n) => new { d, n })
					.Where(dn => dn.n.DocumentRef == id)
					.Select(dn => dn.d)
					.OrderBy(d => d.Id)
					.ToListAsync();

				docs.Sort((d1, d2) => string.Compare(d1.Path, d2.Path, StringComparison.InvariantCultureIgnoreCase));

				rootKey = docs[0].Slug;

				breadcrumbs = new BreadcrumbsItem[docs.Count + 1];
				breadcrumbs[0] = new BreadcrumbsItem() { Path = pathTransformer.Forward(rootKey, "/", false), Title = string.Empty, Document = docs[0].Id };
				breadcrumbs[^1] = new BreadcrumbsItem() { Path = pathTransformer.Forward(rootKey, doc.Path, false), Title = doc.Title, Document = id };

				for (int i = 1; i < docs.Count; i++)
					breadcrumbs[i] = new BreadcrumbsItem() { Path = pathTransformer.Forward(rootKey, docs[i].Path, false), Title = docs[i].Title, Document = docs[i].Id };

				result = new(doc)
				{
					Root = new(docs[0]),
					Parent = new(docs[^1]),
					Breadcrumbs = breadcrumbs
				};

				allDocsIds = [.. docs.Select(d => d.Id), doc.Id];

				if (siblings)
					result.Siblings = await Children(doc.Parent, -1, -1, allowedStatus);
				else
					result.Siblings = [];
			}


			var attrs = await dbContext.DocumentAttributes
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
				result.TotalChildrenCount = await dbContext.Documents.Where(d => d.Parent == id).CountAsync();
				result.Children = await Children(id, childrenFromPos, takeChildren, allowedStatus);
				allDocsIds.AddRange(result.Children.Select(d => d.Id));
			}
			else
			{
				result.Children = [];
			}

			allDocsIds.AddRange(result.Siblings.Where(d => d.Id != doc.Id).Select(d => d.Id));


			var refsList = await dbContext.References
				.AsNoTracking()
				.GroupJoin(dbContext.Documents, r => r.ReferenceTo, d => d.Id, (r, d) => new { r, d })
				.Where(rd => allDocsIds.Contains(rd.r.DocumentRef))
				.SelectMany(
					rd => rd.d.DefaultIfEmpty(),
					(r, d) => new Reference(r.r.Encoded, d.Path, r.r.MediaLink, d.RootSlug, pathTransformer)
				)
				.ToArrayAsync();

			Dictionary<string, string> refs = [];

			foreach (var r in refsList)
				refs.TryAdd(r.Pattern, r.Replacement);


			result.Summary = ReplaceRefs(result.Summary, refs);
			result.CoverPicture = ReplaceRefs(result.CoverPicture, refs);

			Entities.FragmentLink[] links = await dbContext.FragmentLinks
				.AsNoTracking()
				.Include(b => b.Fragment)
				.Where(b => b.DocumentRef == id && b.Enabled)
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


			var fAttrs = await dbContext.FragmentLinks
				.AsNoTracking()
				.Join(dbContext.Fragments, l => l.FragmentRef, f => f.Id, (l, f) => new { l, f })
				.Where(lf => lf.l.DocumentRef == id && lf.l.Enabled)
				.Join(dbContext.FragmentAttributes, lf => lf.f.Id, a => a.FragmentRef, (lf, a) => a)
				.Where(a => a.Enabled)
				.ToArrayAsync();

			foreach (var f in fAttrs)
				f.Value = ReplaceRefs(f.Value, refs);

			result.Fragments = CreateFragmentsTree(links, result, fAttrs.ToLookup(a => a.FragmentRef, a => a), fsr.Fragments);

			return result;
		}

		public Task<Document[]> Children(int id, int childrenFromPos, int take, int[] allowedStatus)
		{
			if (take < 0)
				take = 1000;

			IQueryable<Entities.Document> query;

			if (allowedStatus != null)
				query = dbContext.Documents
					.AsNoTracking()
					.Where(d => d.Parent == id && allowedStatus.Contains(d.PublishStatus) && d.Position >= childrenFromPos)
					.OrderBy(d => d.Position)
					.Take(take);
			else
				query = dbContext.Documents
					.AsNoTracking()
					.Where(d => d.Parent == id && d.PublishStatus == (int)Entities.Document.Status.Published && d.Position >= childrenFromPos)
					.OrderBy(d => d.Position)
					.Take(take);

			return query.Select(d => new Document(d)).ToArrayAsync();
		}

		public async Task<(string, string)> IdToPath(int id)
		{
			var doc = await dbContext.Documents
				.AsNoTracking()
				.FirstOrDefaultAsync(d => d.Id == id);

			if (doc == null)
				return (null, null);

			if (doc.Parent == 0)
				return (doc.Slug, doc.Path);

			var root = await dbContext.Documents
				.AsNoTracking()
				.Join(dbContext.DocumentPathNodes, d => d.Id, n => n.Parent, (d, n) => new { d, n })
				.Where(dn => dn.n.DocumentRef == id && dn.d.Parent == 0)
				.Select(dn => dn.d)
				.OrderBy(d => d.Id)
				.FirstOrDefaultAsync();

			return (root.Slug, doc.Path);
		}

		public async Task<string> UserRole(string login)
		{
			var user = await dbContext.Users
				.AsNoTracking()
				.FirstOrDefaultAsync(u => u.Login == login);

			return user?.Role;
		}

	}
}
