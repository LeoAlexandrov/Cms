using System;
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
using static System.Runtime.InteropServices.JavaScript.JSType;
using Newtonsoft.Json.Linq;


namespace AleProjects.Cms.Application.Services
{

	public partial class ContentManagementService
	{

		public async ValueTask<DtoDocumentAttributeResult> GetDocumentAttribute(int id)
		{
			DocumentAttribute attr = await dbContext.DocumentAttributes.FindAsync(id);

			if (attr == null)
				return null;

			return new(attr);
		}

		public async ValueTask<DtoFragmentAttributeResult> GetFragmentAttribute(int id)
		{
			FragmentAttribute attr = await dbContext.FragmentAttributes.FindAsync(id);

			if (attr == null)
				return null;

			return new(attr);
		}

		/// <summary>
		/// Creates document attribute
		/// </summary>
		public async Task<Result<DtoDocumentAttributeResult>> CreateAttribute(DtoCreateDocumentAttribute dto, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, dto.DocumentRef, "CanManageDocument");

			if (!authResult.Succeeded)
				return Result<DtoDocumentAttributeResult>.Forbidden();

			string key;
			string value;

			authResult = await _authService.AuthorizeAsync(user, "NoInputSanitizing");

			if (!authResult.Succeeded)
			{
				var sanitizer = new HtmlSanitizer();

				key = sanitizer.Sanitize(dto.AttributeKey);
				value = sanitizer.Sanitize(dto.Value);
			}
			else
			{
				key = dto.AttributeKey;
				value = dto.Value;
			}

			Document doc = await dbContext.Documents.FindAsync(dto.DocumentRef);

			if (doc == null)
				return Result<DtoDocumentAttributeResult>.BadParameters("DocumentRef", "No document found");


			DocumentAttribute result = new()
			{
				DocumentRef = dto.DocumentRef,
				AttributeKey = key,
				Value = value,
				Enabled = dto.Enabled,
				Private = dto.Private
			};

			dbContext.DocumentAttributes.Add(result);

			doc.ModifiedAt = DateTimeOffset.UtcNow;
			doc.Author = user.Identity.Name;


			var existingRefs = await dbContext.References
				.Where(r => r.DocumentRef == doc.Id)
				.OrderBy(r => r.ReferenceTo)
				.ThenBy(r => r.MediaLink)
				.ToListAsync();

			string[] xmlData = await dbContext.Fragments
				.Join(dbContext.FragmentLinks, f => f.Id, fl => fl.FragmentRef, (f, fl) => new { fl.Id, fl.DocumentRef, fl.Enabled, f.Data })
				.Where(f => f.DocumentRef == doc.Id && f.Enabled)
				.Select(f => f.Data)
				.ToArrayAsync();

			string[] attrData = await dbContext.DocumentAttributes
				.Where(a => a.DocumentRef == doc.Id && a.Enabled)
				.Select(a => a.Value)
				.ToArrayAsync();

			string[] fAttrData = await dbContext.FragmentLinks
				.Join(dbContext.Fragments, l => l.FragmentRef, f => f.Id, (l, f) => new { l, f })
				.Where(lf => lf.l.DocumentRef == doc.Id && lf.l.Enabled)
				.Join(dbContext.FragmentAttributes, lf => lf.f.Id, a => a.FragmentRef, (lf, a) => a)
				.Where(a => a.Enabled)
				.Select(a => a.Value)
				.ToArrayAsync();

			ReferencesHelper.GetReferencesChanges(doc.Id,
				existingRefs,
				ReferencesHelper.Extract([doc.Summary, doc.CoverPicture, dto.Enabled ? value : null, .. xmlData, .. attrData, .. fAttrData]),
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
				if (dbContext.IsConflict(ex))
					return Result<DtoDocumentAttributeResult>.Conflict("AttributeKey", "Must be unique in this document");

				throw;
			}

			await _notifier.Notify("on_doc_change", doc.RootSlug, doc.Path, doc.Id);

			return Result<DtoDocumentAttributeResult>.Success(new(result));
		}

		/// <summary>
		/// Creates fragment attribute
		/// </summary>
		public async Task<Result<DtoFragmentAttributeResult>> CreateAttribute(DtoCreateFragmentAttribute dto, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, dto.FragmentLinkRef, "CanManageFragment");

			if (!authResult.Succeeded)
				return Result<DtoFragmentAttributeResult>.Forbidden();

			var link = await dbContext.FragmentLinks.FindAsync(dto.FragmentLinkRef);

			if (link == null)
				return Result<DtoFragmentAttributeResult>.BadParameters("FragmentLinkRef", "No fragment link found");

			string key;
			string value;

			authResult = await _authService.AuthorizeAsync(user, "NoInputSanitizing");

			if (!authResult.Succeeded)
			{
				var sanitizer = new HtmlSanitizer();

				key = sanitizer.Sanitize(dto.AttributeKey);
				value = sanitizer.Sanitize(dto.Value);
			}
			else
			{
				key = dto.AttributeKey;
				value = dto.Value;
			}

			Document doc = await dbContext.Documents.FindAsync(link.DocumentRef);

			if (doc == null)
				return Result<DtoFragmentAttributeResult>.BadParameters("DocumentRef", "No document found");

			FragmentAttribute result = new()
			{
				FragmentRef = link.FragmentRef,
				AttributeKey = key,
				Value = value,
				Enabled = dto.Enabled
			};

			dbContext.FragmentAttributes.Add(result);

			doc.ModifiedAt = DateTimeOffset.UtcNow;
			doc.Author = user.Identity.Name;


			var existingRefs = await dbContext.References
				.Where(r => r.DocumentRef == doc.Id)
				.OrderBy(r => r.ReferenceTo)
				.ThenBy(r => r.MediaLink)
				.ToListAsync();

			string[] xmlData = await dbContext.Fragments
				.Join(dbContext.FragmentLinks, f => f.Id, fl => fl.FragmentRef, (f, fl) => new { fl.Id, fl.DocumentRef, fl.Enabled, f.Data })
				.Where(f => f.DocumentRef == doc.Id && f.Enabled)
				.Select(f => f.Data)
				.ToArrayAsync();

			string[] attrData = await dbContext.DocumentAttributes
				.Where(a => a.DocumentRef == doc.Id && a.Enabled)
				.Select(a => a.Value)
				.ToArrayAsync();

			string[] fAttrData = await dbContext.FragmentLinks
				.Join(dbContext.Fragments, l => l.FragmentRef, f => f.Id, (l, f) => new { l, f })
				.Where(lf => lf.l.DocumentRef == doc.Id && lf.l.Enabled)
				.Join(dbContext.FragmentAttributes, lf => lf.f.Id, a => a.FragmentRef, (lf, a) => a)
				.Where(a => a.Enabled)
				.Select(a => a.Value)
				.ToArrayAsync();

			ReferencesHelper.GetReferencesChanges(doc.Id,
				existingRefs,
				ReferencesHelper.Extract([doc.Summary, doc.CoverPicture, dto.Enabled ? value : null, .. xmlData, .. attrData, .. fAttrData]),
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
				if (dbContext.IsConflict(ex))
					return Result<DtoFragmentAttributeResult>.Conflict("AttributeKey", "Must be unique in this document");

				throw;
			}

			await _notifier.Notify("on_doc_update", doc.RootSlug, doc.Path, doc.Id);

			return Result<DtoFragmentAttributeResult>.Success(new(result));
		}

		/// <summary>
		/// Updates document attribute
		/// </summary>
		public async Task<Result<DtoDocumentAttributeResult>> UpdateAttribute(int id, DtoUpdateDocumentAttribute dto, ClaimsPrincipal user)
		{
			var attr = await dbContext.DocumentAttributes.FindAsync(id);

			if (attr == null)
				return Result<DtoDocumentAttributeResult>.NotFound();

			var authResult = await _authService.AuthorizeAsync(user, attr.DocumentRef, "CanManageDocument");

			if (!authResult.Succeeded)
				return Result<DtoDocumentAttributeResult>.Forbidden();


			string value;

			authResult = await _authService.AuthorizeAsync(user, "NoInputSanitizing");

			if (!authResult.Succeeded)
			{
				var sanitizer = new HtmlSanitizer();

				value = sanitizer.Sanitize(dto.Value);
			}
			else
			{
				value = dto.Value;
			}

			attr.Value = value;
			attr.Enabled = dto.Enabled;
			attr.Private = dto.Private;

			Document doc = await dbContext.Documents.FindAsync(attr.DocumentRef);

			doc.ModifiedAt = DateTimeOffset.UtcNow;
			doc.Author = user.Identity.Name;


			var existingRefs = await dbContext.References
				.Where(r => r.DocumentRef == doc.Id)
				.OrderBy(r => r.ReferenceTo)
				.ThenBy(r => r.MediaLink)
				.ToListAsync();

			string[] xmlData = await dbContext.Fragments
				.Join(dbContext.FragmentLinks, f => f.Id, fl => fl.FragmentRef, (f, fl) => new { fl.Id, fl.DocumentRef, fl.Enabled, f.Data })
				.Where(f => f.DocumentRef == doc.Id && f.Enabled)
				.Select(f => f.Data)
				.ToArrayAsync();

			string[] attrData = await dbContext.DocumentAttributes
				.Where(a => a.DocumentRef == doc.Id && a.Id != id && a.Enabled)
				.Select(a => a.Value)
				.ToArrayAsync();

			string[] fAttrData = await dbContext.FragmentLinks
				.Join(dbContext.Fragments, l => l.FragmentRef, f => f.Id, (l, f) => new { l, f })
				.Where(lf => lf.l.DocumentRef == doc.Id && lf.l.Enabled)
				.Join(dbContext.FragmentAttributes, lf => lf.f.Id, a => a.FragmentRef, (lf, a) => a)
				.Where(a => a.Enabled)
				.Select(a => a.Value)
				.ToArrayAsync();

			ReferencesHelper.GetReferencesChanges(doc.Id,
				existingRefs,
				ReferencesHelper.Extract([doc.Summary, doc.CoverPicture, dto.Enabled ? value : null, .. xmlData, .. attrData, .. fAttrData]),
				out List<Reference> toAdd,
				out List<Reference> toRemove);

			if (toAdd != null)
				dbContext.References.AddRange(toAdd);

			if (toRemove != null)
				dbContext.References.RemoveRange(toRemove);


			await dbContext.SaveChangesAsync();

			await _notifier.Notify("on_doc_change", doc.RootSlug, doc.Path, doc.Id);

			return Result<DtoDocumentAttributeResult>.Success(new(attr)); ;
		}

		/// <summary>
		/// Updates fragment attribute
		/// </summary>
		public async Task<Result<DtoFragmentAttributeResult>> UpdateAttribute(int id, DtoUpdateFragmentAttribute dto, ClaimsPrincipal user)
		{
			var attr = await dbContext.FragmentAttributes.FindAsync(id);

			if (attr == null)
				return Result<DtoFragmentAttributeResult>.NotFound();

			var doc = await dbContext.Documents
				.Join(dbContext.FragmentLinks, d => d.Id, l => l.DocumentRef, (d, l) => new { d, l })
				.Where(dl => dl.d.Id == dto.DocumentRef && dl.l.FragmentRef == attr.FragmentRef)
				.Select(dl => dl.d)
				.FirstOrDefaultAsync();

			if (doc == null)
				return Result<DtoFragmentAttributeResult>.BadParameters("DocumentRef", "No document found");

			var authResult = await _authService.AuthorizeAsync(user, doc.Id, "CanManageDocument");

			if (!authResult.Succeeded)
				return Result<DtoFragmentAttributeResult>.Forbidden();


			string value;

			authResult = await _authService.AuthorizeAsync(user, "NoInputSanitizing");

			if (!authResult.Succeeded)
			{
				var sanitizer = new HtmlSanitizer();

				value = sanitizer.Sanitize(dto.Value);
			}
			else
			{
				value = dto.Value;
			}

			attr.Value = value;
			attr.Enabled = dto.Enabled;

			doc.ModifiedAt = DateTimeOffset.UtcNow;
			doc.Author = user.Identity.Name;


			var existingRefs = await dbContext.References
				.Where(r => r.DocumentRef == doc.Id)
				.OrderBy(r => r.ReferenceTo)
				.ThenBy(r => r.MediaLink)
				.ToListAsync();

			string[] xmlData = await dbContext.Fragments
				.Join(dbContext.FragmentLinks, f => f.Id, fl => fl.FragmentRef, (f, fl) => new { fl.Id, fl.DocumentRef, fl.Enabled, f.Data })
				.Where(f => f.DocumentRef == doc.Id && f.Enabled)
				.Select(f => f.Data)
				.ToArrayAsync();

			string[] attrData = await dbContext.DocumentAttributes
				.Where(a => a.DocumentRef == doc.Id && a.Enabled)
				.Select(a => a.Value)
				.ToArrayAsync();

			string[] fAttrData = await dbContext.FragmentLinks
				.Join(dbContext.Fragments, l => l.FragmentRef, f => f.Id, (l, f) => new { l, f })
				.Where(lf => lf.l.DocumentRef == doc.Id && lf.l.Enabled)
				.Join(dbContext.FragmentAttributes, lf => lf.f.Id, a => a.FragmentRef, (lf, a) => a)
				.Where(a => a.Enabled && a.Id != id)
				.Select(a => a.Value)
				.ToArrayAsync();

			ReferencesHelper.GetReferencesChanges(doc.Id,
				existingRefs,
				ReferencesHelper.Extract([doc.Summary, doc.CoverPicture, dto.Enabled ? value : null, .. xmlData, .. attrData, .. fAttrData]),
				out List<Reference> toAdd,
				out List<Reference> toRemove);

			if (toAdd != null)
				dbContext.References.AddRange(toAdd);

			if (toRemove != null)
				dbContext.References.RemoveRange(toRemove);


			await dbContext.SaveChangesAsync();

			await _notifier.Notify("on_doc_update", doc.RootSlug, doc.Path, doc.Id);

			return Result<DtoFragmentAttributeResult>.Success(new(attr)); ;
		}

		/// <summary>
		/// Removes document attribute
		/// </summary>
		public async Task<Result<bool>> DeleteAttribute(int id, ClaimsPrincipal user)
		{
			var attr = await dbContext.DocumentAttributes.FindAsync(id);

			if (attr == null)
				return Result<bool>.NotFound();

			var authResult = await _authService.AuthorizeAsync(user, attr.DocumentRef, "CanManageDocument");

			if (!authResult.Succeeded)
				return Result<bool>.Forbidden();

			Document doc = await dbContext.Documents.FindAsync(attr.DocumentRef);

			doc.ModifiedAt = DateTimeOffset.UtcNow;
			doc.Author = user.Identity.Name;


			var existingRefs = await dbContext.References
				.Where(r => r.DocumentRef == doc.Id)
				.OrderBy(r => r.ReferenceTo)
				.ThenBy(r => r.MediaLink)
				.ToListAsync();

			string[] xmlData = await dbContext.Fragments
				.Join(dbContext.FragmentLinks, f => f.Id, fl => fl.FragmentRef, (f, fl) => new { fl.Id, fl.DocumentRef, fl.Enabled, f.Data })
				.Where(f => f.DocumentRef == doc.Id && f.Enabled)
				.Select(f => f.Data)
				.ToArrayAsync();

			string[] attrData = await dbContext.DocumentAttributes
				.Where(a => a.DocumentRef == doc.Id && a.Id != id && a.Enabled)
				.Select(a => a.Value)
				.ToArrayAsync();

			string[] fAttrData = await dbContext.FragmentLinks
				.Join(dbContext.Fragments, l => l.FragmentRef, f => f.Id, (l, f) => new { l, f })
				.Where(lf => lf.l.DocumentRef == doc.Id && lf.l.Enabled)
				.Join(dbContext.FragmentAttributes, lf => lf.f.Id, a => a.FragmentRef, (lf, a) => a)
				.Where(a => a.Enabled)
				.Select(a => a.Value)
				.ToArrayAsync();

			ReferencesHelper.GetReferencesChanges(doc.Id,
			existingRefs,
				ReferencesHelper.Extract([doc.Summary, doc.CoverPicture, .. xmlData, .. attrData, .. fAttrData]),
				out List<Reference> toAdd,
				out List<Reference> toRemove);

			if (toAdd != null)
				dbContext.References.AddRange(toAdd);

			if (toRemove != null)
				dbContext.References.RemoveRange(toRemove);


			dbContext.DocumentAttributes.Remove(attr);

			await dbContext.SaveChangesAsync();

			await _notifier.Notify("on_doc_change", doc.RootSlug, doc.Path, doc.Id);

			return Result<bool>.Success(true);
		}

		/// <summary>
		/// Removes fragment attribute
		/// </summary>
		public async Task<Result<bool>> DeleteAttribute(int id, int docId, ClaimsPrincipal user)
		{
			var attr = await dbContext.FragmentAttributes.FindAsync(id);

			if (attr == null)
				return Result<bool>.NotFound();

			var doc = await dbContext.Documents
				.Join(dbContext.FragmentLinks, d => d.Id, l => l.DocumentRef, (d, l) => new { d, l })
				.Where(dl => dl.d.Id == docId && dl.l.FragmentRef == attr.FragmentRef)
				.Select(dl => dl.d)
				.FirstOrDefaultAsync();

			if (doc == null)
				return Result<bool>.BadParameters("docId", "No document found");

			var authResult = await _authService.AuthorizeAsync(user, doc.Id, "CanManageDocument");

			if (!authResult.Succeeded)
				return Result<bool>.Forbidden();


			doc.ModifiedAt = DateTimeOffset.UtcNow;
			doc.Author = user.Identity.Name;


			var existingRefs = await dbContext.References
				.Where(r => r.DocumentRef == doc.Id)
				.OrderBy(r => r.ReferenceTo)
				.ThenBy(r => r.MediaLink)
				.ToListAsync();

			string[] xmlData = await dbContext.Fragments
				.Join(dbContext.FragmentLinks, f => f.Id, fl => fl.FragmentRef, (f, fl) => new { fl.Id, fl.DocumentRef, fl.Enabled, f.Data })
				.Where(f => f.DocumentRef == doc.Id && f.Enabled)
				.Select(f => f.Data)
				.ToArrayAsync();

			string[] attrData = await dbContext.DocumentAttributes
				.Where(a => a.DocumentRef == doc.Id && a.Enabled)
				.Select(a => a.Value)
				.ToArrayAsync();

			string[] fAttrData = await dbContext.FragmentLinks
				.Join(dbContext.Fragments, l => l.FragmentRef, f => f.Id, (l, f) => new { l, f })
				.Where(lf => lf.l.DocumentRef == doc.Id && lf.l.Enabled)
				.Join(dbContext.FragmentAttributes, lf => lf.f.Id, a => a.FragmentRef, (lf, a) => a)
				.Where(a => a.Enabled && a.Id != id)
				.Select(a => a.Value)
				.ToArrayAsync();

			ReferencesHelper.GetReferencesChanges(doc.Id,
				existingRefs,
				ReferencesHelper.Extract([doc.Summary, doc.CoverPicture, .. xmlData, .. attrData, .. fAttrData]),
				out List<Reference> toAdd,
				out List<Reference> toRemove);

			if (toAdd != null)
				dbContext.References.AddRange(toAdd);

			if (toRemove != null)
				dbContext.References.RemoveRange(toRemove);


			dbContext.FragmentAttributes.Remove(attr);

			await dbContext.SaveChangesAsync();

			await _notifier.Notify("on_doc_update", doc.RootSlug, doc.Path, doc.Id);

			return Result<bool>.Success(true);
		}

	}

}