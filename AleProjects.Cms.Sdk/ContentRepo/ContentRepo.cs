using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using AleProjects.Base64;
using AleProjects.Cms.Application.Services;
using Entities = AleProjects.Cms.Domain.Entities;
using AleProjects.Cms.Domain.ValueObjects;
using AleProjects.Cms.Infrastructure.Data;
using AleProjects.Cms.Sdk.ViewModels;


namespace AleProjects.Cms.Sdk.ContentRepo
{

	public class ContentRepo : IDisposable
	{
		readonly CmsDbContext dbContext;
		readonly string mediaHost;
		readonly static FragmentSchemaService fss = new();
		readonly static object lockObject = new();
		static int? schemataChecksum = null;
		bool disposed;

		struct Reference
		{
			public string Pattern { get; set; }
			public string Replacement { get; set; }

			public Reference(int referenceTo, string path, string mediaLink, string mediaHost)
			{
				if (string.IsNullOrEmpty(mediaLink))
				{
					Pattern = string.Format("#({0})", referenceTo);
					Replacement = path;
				}
				else
				{
					Pattern = string.Format("#('{0}')", Base64Url.Encode(mediaLink));
					Replacement = mediaHost + mediaLink;
				}
			}
		}

		public ContentRepo(IConfiguration configuration)
		{
			string connString = configuration.GetConnectionString("CmsDbConnection");
			var contextOptions = new DbContextOptionsBuilder<CmsDbContext>().UseSqlServer(connString).Options;
			dbContext = new CmsDbContext(contextOptions);

			mediaHost = configuration.GetValue<string>("MediaHost");

			if (!mediaHost.EndsWith('/'))
				mediaHost += "/";

			LoadFragmentSchemaService(dbContext);
		}

		#region private-functions

		private static void LoadFragmentSchemaService(CmsDbContext dbContext)
		{
			var cs = dbContext.Database
				.SqlQueryRaw<int>($"SELECT CHECKSUM_AGG(BINARY_CHECKSUM(ModifiedAt)) AS [Value] FROM Schemata WITH (NOLOCK)")
				.FirstOrDefault();

			if (!schemataChecksum.HasValue || cs != schemataChecksum)
				lock (lockObject)
					if (!schemataChecksum.HasValue || cs != schemataChecksum)
					{
						schemataChecksum = cs;
						fss.Reload(dbContext);
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

		#endregion


		public async Task<Document> GetDocument(string root, string path, bool children, bool siblings)
		{
			var doc = await dbContext.Documents
				.AsNoTracking()
				.Where(d => d.Parent == 0 && d.Slug == root && d.Published)
				.FirstOrDefaultAsync();

			if (doc == default)
				return null;

			int rootId = doc.Id;

			Document result;
			BreadcrumbsItem[] breadcrumbs;

			if (string.IsNullOrEmpty(path) || path == "/")
			{
				breadcrumbs = [new BreadcrumbsItem() { Path = "/", Title = string.Empty }];

				result = new(doc)
				{
					Breadcrumbs = breadcrumbs
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

				doc = docs.FirstOrDefault(d => string.Compare(d.Path, path, StringComparison.InvariantCultureIgnoreCase) == 0);

				if (EqualityComparer<Entities.Document>.Default.Equals(doc, default))
					return null;

				Document parent = null;
				int[] ids = docs.Select(d => d.Id).ToArray();
				int id = doc.Parent;

				breadcrumbs = new BreadcrumbsItem[slugs.Length];

				breadcrumbs[^1] = new BreadcrumbsItem() { Path = doc.Path, Title = doc.Title };

				for (int i = 2; i < slugs.Length; i++)
				{
					int j = Array.BinarySearch(ids, id);

					id = docs[j].Parent;
					breadcrumbs[^i] = new BreadcrumbsItem() { Path = docs[j].Path, Title = docs[j].Title };

					if (i == 2)
						parent = new Document(docs[j]);
				}

				breadcrumbs[0] = new BreadcrumbsItem() { Path = "/", Title = string.Empty };

				result = new(doc)
				{
					Parent = parent,
					Breadcrumbs = breadcrumbs
				};

				if (siblings)
					result.Siblings = await Children(doc.Parent);
				else
					result.Siblings = [];
			}


			result.Attributes = await dbContext.DocumentAttributes
				.AsNoTracking()
				.Where(a => a.DocumentRef == doc.Id && a.Enabled)
				.OrderBy(a => a.AttributeKey)
				.ToDictionaryAsync(a => a.AttributeKey, a => a.Value);


			if (children)
				result.Children = await Children(doc.Id);
			else
				result.Children = [];

			var refs = await dbContext.References
				.AsNoTracking()
				.GroupJoin(dbContext.Documents, r => r.ReferenceTo, d => d.Id, (r, d) => new { r, d })
				.Where(rd => rd.r.DocumentRef == doc.Id)
				.SelectMany(
					rd => rd.d.DefaultIfEmpty(),
					(r, d) => new Reference(r.r.ReferenceTo, d.Path, r.r.MediaLink, mediaHost))
				.ToDictionaryAsync(r => r.Pattern, r => r.Replacement);

			result.Summary = ReferencesHelper.Replace(result.Summary, refs);
			result.CoverPicture = ReferencesHelper.Replace(result.CoverPicture, refs);

			Entities.FragmentLink[] links = await dbContext.FragmentLinks
				.AsNoTracking()
				.Include(b => b.Fragment)
				.Where(b => b.DocumentRef == doc.Id && b.Enabled)
				.OrderBy(b => b.ContainerRef)
				.ThenBy(b => b.Position)
				.ToArrayAsync();

			foreach (var link in links)
				link.Fragment.Data = ReferencesHelper.Replace(link.Fragment.Data, refs);

			var attrs = await dbContext.FragmentLinks
				.AsNoTracking()
				.Join(dbContext.Fragments, l => l.FragmentRef, f => f.Id, (l, f) => new { l, f })
				.Where(lf => lf.l.DocumentRef == doc.Id && lf.l.Enabled)
				.Join(dbContext.FragmentAttributes, lf => lf.f.Id, a => a.FragmentRef, (lf, a) => a)
				.Where(a => a.Enabled)
				.ToArrayAsync();


			result.Fragments = CreateFragmentsTree(links, result, attrs.ToLookup(a => a.FragmentRef, a => a), fss.Fragments);

			return result;
		}

		public Task<Document[]> Children(int docId)
		{
			return dbContext.Documents
				.AsNoTracking()
				.Where(d => d.Parent == docId && d.Published)
				.OrderBy(d => d.Position)
				.Select(d => new Document(d))
				.ToArrayAsync();
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
