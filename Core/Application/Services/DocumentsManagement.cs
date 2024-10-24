﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using Ganss.Xss;

using AleProjects.Cms.Application.Dto;
using AleProjects.Cms.Domain.Entities;
using AleProjects.Cms.Domain.ValueObjects;
using AleProjects.Cms.Infrastructure.Data;


namespace AleProjects.Cms.Application.Services
{

	public partial class ContentManagementService(CmsDbContext dbContext, FragmentSchemaService schemaService, IAuthorizationService authService)
	{
		private readonly CmsDbContext dbContext = dbContext;
		private readonly FragmentSchemaService _schemaService = schemaService;
		private readonly IAuthorizationService _authService = authService;


		#region private-functions

		private static void SetChildren<T,U>(DtoTreeNode<T> doc, Dictionary<T, Memory<U>> tree) where U : ITreeNode<T>
		{
			if (tree.TryGetValue(doc.Id, out Memory<U> children))
			{
				Span<U> span = children.Span;
				DtoTreeNode<T>[] docChildren = new DtoTreeNode<T>[span.Length];

				for (int i = 0; i < span.Length; i++)
					SetChildren(docChildren[i] = DtoTreeNode<T>.Create(span[i]), tree);

				doc.Children = docChildren;
				doc.Expandable = true;
			}
		}

		private static DtoTreeNode<T>[] CreateTree<T,U>(U[] docs) where U : ITreeNode<T>
		{
			Dictionary<T, Memory<U>> tree = [];

			if (docs.Length > 0)
			{
				T key = docs[0].Parent;
				int from = 0;

				for (int i = 0; i < docs.Length; i++)
					if (!key.Equals(docs[i].Parent))
					{
						tree.Add(key, docs.AsMemory(from, i - from));
						from = i;
						key = docs[i].Parent;
					}

				tree.Add(key, docs.AsMemory(from, docs.Length - from));
			}

			DtoTreeNode<T>[] result;

			if (tree.TryGetValue(default, out Memory<U> roots))
			{
				result = roots
					.ToArray()
					.Select(DtoTreeNode<T>.Create)
					.ToArray();

				foreach (var d in result)
					SetChildren(d, tree);
			}
			else
			{
				result = [];
			}

			return result;
		}

		#endregion

		public async Task<DtoTreeNode<int>[]> DocumentsTree()
		{
			Document[] docs = await dbContext.Documents
				.AsNoTracking()
				.OrderBy(d => d.Parent)
				.ThenBy(d => d.Position)
				.ToArrayAsync();

			var result = CreateTree<int, Document>(docs);

			return result;
		}

		public async Task<DtoTreeNode<int>[]> DocumentsTree(int docId)
		{
			if (docId <= 0)
				return await DocumentsTree();

			Document[] docs = await dbContext.Documents
				.AsNoTracking()
				.Join(dbContext.DocumentPathNodes, d => d.Id, n => n.DocumentRef, (d, n) => new { d, n })
				.Where(dn => dn.d.Id == docId || dn.n.Parent == docId)
				.Select(dn => dn.d)
				.OrderBy(d => d.Parent)
				.ThenBy(d => d.Position)
				.ToArrayAsync();

			var result = CreateTree<int, Document>(docs);

			return result;
		}

		public async Task<DtoFullDocumentResult> GetDocument(int id)
		{
			Document doc = await dbContext.Documents.FindAsync(id);

			if (doc == null)
				return null;

			FragmentLink[] links = await dbContext.FragmentLinks
				.AsNoTracking()
				.Include(b => b.Fragment)
				.Where(b => b.DocumentRef == id)
				.OrderBy(b => b.ContainerRef)
				.ThenBy(b => b.Position)
				.ToArrayAsync();

			XSElement xse;

			for (int i = 0; i < links.Length; i++)
				if ((xse = _schemaService.Find(links[i].Fragment.XmlSchema + ":" + links[i].Fragment.XmlName)) != null && xse.RepresentsContainer)
					links[i].Data = "container";

			DocumentAttribute[] attrs = await dbContext.DocumentAttributes
				.AsNoTracking()
				.Where(a => a.DocumentRef == id)
				.OrderBy(a => a.AttributeKey)
				.ToArrayAsync();

			DtoFullDocumentResult result = new()
			{ 
				Properties = new(doc), 
				FragmentLinks = links.Select(l => new DtoFragmentLinkResult(l)).ToArray(),
				Attributes = attrs.Select(a => new DtoDocumentAttributeResult(a)).ToArray(),
				FragmentsTree = CreateTree<int, FragmentLink>(links)
			};

			return result;
		}

		public async Task<Result<DtoDocumentResult>> CreateDocument(DtoCreateDocument dto, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, "NoInputSanitizing");

			string slug;
			string title;

			if (!authResult.Succeeded)
			{
				var sanitizer = new HtmlSanitizer();

				slug = sanitizer.Sanitize(dto.Slug);
				title = sanitizer.Sanitize(dto.Title);
			}
			else
			{
				slug = dto.Slug;
				title = dto.Title;
			}


			string language;
			string icon;
			bool published;
			string path;
			List<DocumentPathNode> pathNodes;

			if (dto.Parent > 0)
			{
				var parent = await dbContext.Documents
					.AsNoTracking()
					.Include(d => d.DocumentPathNodes.OrderBy(n => n.Position))
					.FirstOrDefaultAsync(d => d.Id == dto.Parent);

				if (parent == null)
					return Result<DtoDocumentResult>.BadParameters("Parent", "No parent document found");

				path = parent.Path + "/" + slug;
				language = parent.Language;
				icon = "article";
				published = parent.Published;
				pathNodes = new(parent.DocumentPathNodes.Select(n => new DocumentPathNode() { Parent = n.Parent, Position = n.Position }));

				pathNodes.Add(new() { Parent = dto.Parent, Position = pathNodes.Count });
			}
			else
			{
				path = ""; // slug;
				language = "en-US";
				icon = "home";
				published = true;
				pathNodes = null;
			}

			int position = await dbContext.Documents.CountAsync(d => d.Parent == dto.Parent);
			DateTimeOffset now = DateTimeOffset.UtcNow;

			Document result = new()
			{
				//Id = id,
				Parent = dto.Parent,
				Position = position,
				Slug = slug,
				Path = path,
				Title = title,
				Language = language,
				Icon = icon,
				Published = published,
				Author = user.Identity.Name,
				CreatedAt = now,
				ModifiedAt = now,
				DocumentPathNodes = pathNodes
			};

			dbContext.Documents.Add(result);

			try
			{
				await dbContext.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
					return Result<DtoDocumentResult>.Conflict("Slug", "Must be unique under parent document");

				throw;
			}

			return Result<DtoDocumentResult>.Success(new(result));
		}

		public async Task<Result<DtoDocumentResult>> UpdateDocument(int id, DtoUpdateDocument dto, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, id, "CanManageDocument");

			if (!authResult.Succeeded)
				return Result<DtoDocumentResult>.Forbidden();

			string slug;
			string title;
			string summary;
			string picture;
			string icon;
			string description;

			authResult = await _authService.AuthorizeAsync(user, "NoInputSanitizing");

			if (!authResult.Succeeded)
			{
				var sanitizer = new HtmlSanitizer();

				slug = sanitizer.Sanitize(dto.Slug);
				title = sanitizer.Sanitize(dto.Title);
				summary = sanitizer.Sanitize(dto.Summary);
				picture = sanitizer.Sanitize(dto.CoverPicture);
				icon = sanitizer.Sanitize(dto.Icon);
				description = sanitizer.Sanitize(dto.Description);
			}
			else
			{
				slug = dto.Slug;
				title = dto.Title;
				summary = dto.Summary;
				picture = dto.CoverPicture;
				icon = dto.Icon;
				description = dto.Description;
			}


			var doc = await dbContext.Documents.FindAsync(id);

			if (doc == null)
				return Result<DtoDocumentResult>.NotFound();

			bool slugChanged = doc.Slug != slug;
			bool needsChildrenUpdate = slugChanged || (!dto.Published && doc.Published);

			Document[] children = needsChildrenUpdate ?
				await dbContext.Documents
						.Join(dbContext.DocumentPathNodes, d => d.Id, n => n.DocumentRef, (d, n) => new { d, n })
						.Where(dn => dn.n.Parent == id)
						.Select(dn => dn.d)
						.ToArrayAsync() :
				null;

			if (children != null && children.Length > 0)
			{
				authResult = await _authService.AuthorizeAsync(user, "IsDeveloper");

				if (!authResult.Succeeded)
					return Result<DtoDocumentResult>.Forbidden();
			}


			if (doc.Published != dto.Published)
				if (dto.Published)
				{
					var parent = await dbContext.Documents.FindAsync(doc.Parent);

					if (parent != null && !parent.Published)
						return Result<DtoDocumentResult>.BadParameters("Published", "Parent document is not published");
				}
				else
				{
					for (int i = 0; i < children.Length; i++)
						children[i].Published = false;
				}

			if (slugChanged)
				if (doc.Parent > 0)
				{
					string[] pathItems = doc.Path.Split('/');

					pathItems[^1] = slug;

					string oldPath = doc.Path;
					int l = oldPath.Length;
					string newPath = string.Join("/", pathItems);

					doc.Slug = slug;
					doc.Path = newPath;

					for (int i = 0; i < children.Length; i++)
						children[i].Path = newPath + children[i].Path[l..];
				}
				else
				{
					doc.Slug = slug;
					doc.Path = "";
				}


			doc.Title = title;
			doc.Summary = summary;
			doc.CoverPicture = picture;
			doc.Description = description;
			doc.Language = dto.Language;
			doc.Icon = icon;
			doc.AssociatedClaims = dto.AssociatedClaims;
			doc.Published = dto.Published;
			doc.Author = user.Identity.Name;
			doc.ModifiedAt = DateTimeOffset.UtcNow;


			var existingRefs = await dbContext.References
				.Where(r => r.DocumentRef == id)
				.OrderBy(r => r.ReferenceTo)
				.ToListAsync();

			string[] xmlData = await dbContext.Fragments
				.Join(dbContext.FragmentLinks, f => f.Id, fl => fl.FragmentRef, (f, fl) => new { fl.Id, fl.DocumentRef, fl.Enabled, f.Data })
				.Where(f => f.DocumentRef == id && f.Enabled)
				.Select(f => f.Data)
				.ToArrayAsync();

			ReferencesHelper.GetReferencesChanges(id,
				existingRefs,
				ReferencesHelper.Extract([summary, picture, .. xmlData]), 
				out List<Reference> toAdd,
				out List<Reference> toRemove);

			if (toAdd != null)
				dbContext.References.AddRange(toAdd);

			if (toRemove != null)
				dbContext.References.RemoveRange(toRemove);


			try
			{
				await dbContext.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
					return Result<DtoDocumentResult>.Conflict("Slug", "Must be unique under parent document");

				throw;
			}

			return Result<DtoDocumentResult>.Success(new(doc)); ;
		}

		public async Task<Result<bool>> DeleteDocument(int id, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, id, "CanManageDocument");

			if (!authResult.Succeeded)
				return Result<bool>.Forbidden();

			var doc = await dbContext.Documents.FindAsync(id);

			if (doc == null)
				return Result<bool>.NotFound();

			int position = doc.Position;
			int parent = doc.Parent;
			int docId = doc.Id;

			var children = await dbContext.Documents
				.Join(dbContext.DocumentPathNodes, d => d.Id, n => n.DocumentRef, (d, n) => new { d, n })
				.Where(dn => dn.n.Parent == id)
				.Select(dn => dn.d)
				.ToArrayAsync();

			if (children.Length > 0)
			{
				authResult = await _authService.AuthorizeAsync(user, "IsDeveloper");

				if (!authResult.Succeeded)
					return Result<bool>.Forbidden();
			}

			var fragmentsToRemove = await dbContext.Fragments
				.Join(dbContext.FragmentLinks, f => f.Id, fl => fl.FragmentRef, (f, fl) => new { f, fl })
				.Where(ffl => ffl.fl.DocumentRef == docId && !ffl.f.Shared)
				.Select(ffl => ffl.f)
				.ToListAsync();

			if (children.Length != 0)
			{
				for (int i = 0; i < children.Length; i++)
				{
					var d = children[i].Id;

					var fragments = dbContext.Fragments
						.Join(dbContext.FragmentLinks, f => f.Id, fl => fl.FragmentRef, (f, fl) => new { f, fl })
						.Where(ffl => ffl.fl.DocumentRef == d && !ffl.f.Shared)
						.Select(ffl => ffl.f);

					fragmentsToRemove.AddRange(fragments);
				}

				dbContext.Documents.RemoveRange(children);
			}

			var siblingsAfter = await dbContext.Documents
				.Where(d => d.Parent == parent && d.Position > position)
				.ToArrayAsync();

			foreach (var d in siblingsAfter)
				d.Position--;

			dbContext.Documents.Remove(doc);

			if (fragmentsToRemove.Count > 0)
				dbContext.Fragments.RemoveRange(fragmentsToRemove);

			await dbContext.SaveChangesAsync();

			return Result<bool>.Success(true);
		}

		public async Task<Result<DtoDocumentResult>> LockDocument(int id, bool lockState, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, id, "CanManageDocument");

			if (!authResult.Succeeded)
				return Result<DtoDocumentResult>.Forbidden();

			var doc = await dbContext.Documents.FindAsync(id);

			if (doc == null)
				return Result<DtoDocumentResult>.NotFound();

			doc.EditorRoleRequired = lockState ? user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value : null;
			doc.Author = user.Identity.Name;
			doc.ModifiedAt = DateTimeOffset.UtcNow;

			await dbContext.SaveChangesAsync();

			return Result<DtoDocumentResult>.Success(new(doc));
		}

		public async Task<Result<DtoDocumentResult>> SetParentDocument(int id, int parentId, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, id, "CanManageDocument");

			if (!authResult.Succeeded)
				return Result<DtoDocumentResult>.Forbidden();

			Document doc = await dbContext.Documents.FindAsync(id);

			if (doc == null)
				return Result<DtoDocumentResult>.NotFound();

			if (parentId == doc.Parent)
				return Result<DtoDocumentResult>.Success(new(doc));


			var children = await dbContext.Documents
				.Join(dbContext.DocumentPathNodes, d => d.Id, n => n.DocumentRef, (d, n) => new { d, n })
				.Where(dn => dn.n.Parent == id)
				.Select(dn => dn.d)
				.Include(d => d.DocumentPathNodes.OrderBy(n => n.Position))
				.ToArrayAsync();

			if (children.Length > 0)
			{
				authResult = await _authService.AuthorizeAsync(user, "IsDeveloper");

				if (!authResult.Succeeded)
					return Result<DtoDocumentResult>.Forbidden();
			}


			Document newParent;

			if (parentId > 0)
			{
				newParent = await dbContext.Documents
					.AsNoTracking()
					.Include(d => d.DocumentPathNodes.OrderBy(n => n.Position))
					.FirstOrDefaultAsync(d => d.Id == parentId);

				if (newParent == null)
					return Result<DtoDocumentResult>.BadParameters("Parent", "Parent document not found");

				if (children.Any(d => d.Id == parentId))
					return Result<DtoDocumentResult>.BadParameters("Parent", "Parent is invalid");
			}
			else
			{
				newParent = null;
			}


			// fix and set positions

			int oldPosition = doc.Position;
			var oldParent = doc.Parent;

			var siblingsAfter = await dbContext.Documents
				.Where(d => d.Parent == oldParent && d.Position > oldPosition)
				.ToArrayAsync();

			foreach (var d in siblingsAfter)
				d.Position--;

			int newPosition = await dbContext.Documents
				.CountAsync(d => d.Parent == parentId);

			string oldPath = doc.Path;
			string newPath = newParent != null ? newParent.Path + "/" + doc.Slug : ""; // doc.Slug;

			doc.Path = newPath;
			doc.Parent = parentId;
			doc.Position = newPosition;
			doc.Author = user.Identity.Name;
			doc.ModifiedAt = DateTimeOffset.UtcNow;

			// end fix and set positions


			var docPathNodes = await dbContext.DocumentPathNodes
				.Where(dn => dn.DocumentRef == id)
				.OrderBy(dn => dn.Position)
				.ToListAsync();

			int diff;

			if (newParent != null)
			{
				int k = newParent.DocumentPathNodes.Count;

				diff = 1 + k - docPathNodes.Count;

				if (diff > 0)
				{
					for (int i = 0; i < diff; i++)
						docPathNodes.Add(new DocumentPathNode() { DocumentRef = id });

					dbContext.DocumentPathNodes.AddRange(docPathNodes.TakeLast(diff));
				}
				else if (diff < 0)
				{
					dbContext.DocumentPathNodes.RemoveRange(docPathNodes.TakeLast(-diff));
					docPathNodes.RemoveRange(k, -diff);
				}


				for (int i = 0; i < k; i++)
				{
					docPathNodes[i].Parent = newParent.DocumentPathNodes[i].Parent;
					docPathNodes[i].Position = i;
				}

				docPathNodes[k].Parent = parentId;
				docPathNodes[k].Position = k;
			}
			else
			{
				diff = -docPathNodes.Count;
				dbContext.DocumentPathNodes.RemoveRange(docPathNodes);
				docPathNodes.Clear();
			}


			var childrenD = children.ToDictionary(c => c.Id, c => c);
			int l = oldPath.Length;
			int n = docPathNodes.Count;

			for (int i = 0; i < children.Length; i++)
			{
				var child = children[i];

				child.Path = newPath + child.Path[l..];

				if (diff > 0)
				{
					for (int j = 0; j < diff; j++)
						child.DocumentPathNodes.Add(new DocumentPathNode() { DocumentRef = child.Id });

					dbContext.DocumentPathNodes.AddRange(child.DocumentPathNodes.TakeLast(diff));
				}
				else if (diff < 0)
				{
					dbContext.DocumentPathNodes.RemoveRange(child.DocumentPathNodes.TakeLast(-diff));
					child.DocumentPathNodes.RemoveRange(child.DocumentPathNodes.Count + diff, -diff);
				}

				for (int j = 0; j < n; j++)
				{
					child.DocumentPathNodes[j].Parent = docPathNodes[j].Parent;
					child.DocumentPathNodes[j].Position = j;
				}

				child.DocumentPathNodes[n].Parent = doc.Id;
				child.DocumentPathNodes[n].Position = n;

				var parent = child.Parent;

				for (int j = child.DocumentPathNodes.Count - 1; j > n; j--)
				{
					child.DocumentPathNodes[j].Position = j;
					child.DocumentPathNodes[j].Parent = parent;

					var p = childrenD[parent];
					parent = p.Parent;
				}

			}

			try
			{
				await dbContext.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
					return Result<DtoDocumentResult>.Conflict("Parent", "Slug must be unique under parent document");

				throw;
			}


			return Result<DtoDocumentResult>.Success(new(doc));
		}

		public async Task<Result<DtoMoveDocumentResult>> MoveDocument(int id, int posIncrement, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, id, "CanManageDocument");

			if (!authResult.Succeeded)
				return Result<DtoMoveDocumentResult>.Forbidden();

			var doc = await dbContext.Documents.FindAsync(id);

			if (doc == null)
				return Result<DtoMoveDocumentResult>.NotFound();

			int parent = doc.Parent;

			var siblings = await dbContext.Documents
				.Where(d => d.Parent == parent)
				.OrderBy(d => d.Position)
				.ToArrayAsync();

			int oldPosition = doc.Position;
			int newPosition = oldPosition + posIncrement;

			if (newPosition < 0)
				newPosition = 0;
			else if (newPosition >= siblings.Length)
				newPosition = siblings.Length - 1;

			if (newPosition != oldPosition)
			{
				doc.Position = newPosition;
				doc.Author = user.Identity.Name;
				doc.ModifiedAt = DateTimeOffset.UtcNow;

				if (posIncrement < 0)
				{
					for (int i = newPosition; i < oldPosition; i++)
						siblings[i].Position++;
				}
				else
				{
					for (int i = oldPosition+1; i <= newPosition; i++)
						siblings[i].Position--;
				}

				await dbContext.SaveChangesAsync();
			}

			return Result<DtoMoveDocumentResult>.Success(
				new DtoMoveDocumentResult() 
				{ 
					NewPosition = newPosition, 
					OldPosition = oldPosition,
					Author = doc.Author, 
					ModifiedAt = doc.ModifiedAt 
				});
		}

		public async Task<Result<DtoDocumentResult>> CopyDocument(int originId, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, originId, "CanManageDocument");

			if (!authResult.Succeeded)
				return Result<DtoDocumentResult>.Forbidden();

			Document origin = await dbContext.Documents
				.AsNoTracking()
				.Include(d => d.DocumentPathNodes.OrderBy(n => n.Position))
				.Include(d => d.References.OrderBy(r => r.ReferenceTo))
				.FirstOrDefaultAsync(d => d.Id == originId);

			if (origin == null)
				return Result<DtoDocumentResult>.BadParameters("Origin", "Original document not found");

			DateTimeOffset now = DateTimeOffset.UtcNow;
			int position = await dbContext.Documents.CountAsync(d => d.Parent == origin.Parent);

			string[] pathItems = origin.Path.Split('/');
			string newSlug = string.Format("{0}-{1}", origin.Slug, now.Ticks);

			if (origin.Parent > 0)
				pathItems[^1] = newSlug;

			Document result = new()
			{
				Slug = newSlug,
				Path = string.Join('/', pathItems),
				Title = string.Format("{0}-copy", origin.Title),
				Parent = origin.Parent,
				Position = position,
				Summary = origin.Summary,
				CoverPicture = origin.CoverPicture,
				Language = origin.Language,
				Description = origin.Description,
				Icon = origin.Icon,
				AssociatedClaims = origin.AssociatedClaims,
				Published = origin.Published,
				CreatedAt = now,
				ModifiedAt = now,
				EditorRoleRequired = origin.EditorRoleRequired,
				Author = user.Identity.Name
			};

			if (origin.DocumentPathNodes.Count != 0)
				result.DocumentPathNodes = new(origin.DocumentPathNodes.Select(n => new DocumentPathNode() { Parent = n.Parent, Position = n.Position }));

			if (origin.References.Count != 0)
				result.References = new(origin.References.Select(r => new Reference() { ReferenceTo = r.ReferenceTo }));

			dbContext.Documents.Add(result);

			await dbContext.SaveChangesAsync();

			return Result<DtoDocumentResult>.Success(new(result));
		}
	}

}