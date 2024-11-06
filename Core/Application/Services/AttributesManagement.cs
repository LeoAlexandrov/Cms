using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

using Ganss.Xss;

using AleProjects.Cms.Application.Dto;
using AleProjects.Cms.Domain.Entities;
using System.Linq;
using Microsoft.EntityFrameworkCore;


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
				Enabled = dto.Enabled
			};

			dbContext.DocumentAttributes.Add(result);

			doc.ModifiedAt = DateTimeOffset.UtcNow;
			doc.Author = user.Identity.Name;

			try
			{
				await dbContext.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
					return Result<DtoDocumentAttributeResult>.Conflict("AttributeKey", "Must be unique in this document");

				throw;
			}

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

			try
			{
				await dbContext.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
					return Result<DtoFragmentAttributeResult>.Conflict("AttributeKey", "Must be unique in this document");

				throw;
			}

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

			Document doc = await dbContext.Documents.FindAsync(attr.DocumentRef);

			doc.ModifiedAt = DateTimeOffset.UtcNow;
			doc.Author = user.Identity.Name;

			await dbContext.SaveChangesAsync();
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

			await dbContext.SaveChangesAsync();
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

			dbContext.DocumentAttributes.Remove(attr);

			await dbContext.SaveChangesAsync();

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

			dbContext.FragmentAttributes.Remove(attr);

			await dbContext.SaveChangesAsync();

			return Result<bool>.Success(true);
		}

	}

}