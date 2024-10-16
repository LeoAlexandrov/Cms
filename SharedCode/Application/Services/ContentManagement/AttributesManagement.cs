using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

using Ganss.Xss;

using AleProjects.Cms.Application.Dto;
using AleProjects.Cms.Domain.Entities;


namespace AleProjects.Cms.Application.Services
{

	public partial class ContentManagementService
	{

		public async ValueTask<DtoDocumentAttributeResult> GetAttribute(int id)
		{
			DocumentAttribute attr = await dbContext.DocumentAttributes.FindAsync(id);

			if (attr == null)
				return null;

			return new(attr);
		}

		public async Task<CreateAttributeResult> CreateAttribute(DtoCreateDocumentAttribute dto, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, dto.DocumentRef, "CanManageDocument");

			if (!authResult.Succeeded)
				return CreateAttributeResult.AccessForbidden();

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
				return CreateAttributeResult.BadAttributeParameters(ModelErrors.Create("DocumentRef", "No document found"));


			DocumentAttribute result = new()
			{
				//Id = id,
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
					return CreateAttributeResult.AttributeConflict(ModelErrors.Create("AttributeKey", "Must be unique in this document"));

				throw;
			}

			return CreateAttributeResult.Success(new(result));
		}

		public async Task<UpdateAttributeResult> UpdateAttribute(int id, DtoUpdateDocumentAttribute dto, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, id, "CanManageAttribute");

			if (!authResult.Succeeded)
				return UpdateAttributeResult.AccessForbidden();

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

			var attr = await dbContext.DocumentAttributes.FindAsync(id);

			if (attr == null)
				return UpdateAttributeResult.AttributeNotFound();

			attr.Value = value;
			attr.Enabled = dto.Enabled;

			Document doc = await dbContext.Documents.FindAsync(attr.DocumentRef);

			doc.ModifiedAt = DateTimeOffset.UtcNow;
			doc.Author = user.Identity.Name;

			await dbContext.SaveChangesAsync();
			return UpdateAttributeResult.Success(new(attr)); ;
		}

		public async Task<DeleteAttributeResult> DeleteAttribute(int id, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, id, "CanManageAttribute");

			if (!authResult.Succeeded)
				return DeleteAttributeResult.AccessForbidden();

			var attr = await dbContext.DocumentAttributes.FindAsync(id);

			if (attr == null)
				return DeleteAttributeResult.AttributeNotFound();

			Document doc = await dbContext.Documents.FindAsync(attr.DocumentRef);

			doc.ModifiedAt = DateTimeOffset.UtcNow;
			doc.Author = user.Identity.Name;

			dbContext.DocumentAttributes.Remove(attr);

			await dbContext.SaveChangesAsync();

			return DeleteAttributeResult.Success();
		}

	}

}