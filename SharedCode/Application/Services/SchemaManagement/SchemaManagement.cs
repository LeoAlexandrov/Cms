using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Xml.Schema;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using AleProjects.Cms.Application.Dto;
using AleProjects.Cms.Domain.Entities;
using AleProjects.Cms.Domain.ValueObjects;
using AleProjects.Cms.Infrastructure.Data;


namespace AleProjects.Cms.Application.Services
{

	public partial class SchemaManagementService(CmsDbContext dbContext, IAuthorizationService authService)
	{
		private readonly CmsDbContext dbContext = dbContext;
		private readonly IAuthorizationService _authService = authService;


		public Task<DtoSchemaResult[]> Schemata()
		{
			return dbContext.Schemata
				.AsNoTracking()
				.OrderBy(s => s.Namespace)
				.Select(s => new DtoSchemaResult(s))
				.ToArrayAsync();
		}

		public async Task<DtoSchemaResult> GetSchema(int id)
		{
			Schema schema = await dbContext.Schemata.FindAsync(id);

			if (schema == null)
				return null;

			DtoSchemaResult result = new(schema);

			return result;
		}

		public async Task<CreateSchemaResult> CreateSchema(DtoCreateSchema dto, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, "IsAdmin");

			if (!authResult.Succeeded)
				return CreateSchemaResult.AccessForbidden();

			string ns = "http://h-cms.net/cms/new-schema.xsd";

			string data = @"<?xml version=""1.0"" encoding=""utf-8""?>

<xs:schema
	targetNamespace=""http://h-cms.net/cms/new-schema.xsd""
	elementFormDefault=""qualified""
	xmlns=""http://h-cms.net/cms/new-schema.xsd""
	xmlns:xs=""http://www.w3.org/2001/XMLSchema"">

</xs:schema>";


			var schema = new Schema()
			{
				Namespace = ns,
				Data = data,
				Description = dto.Description,
				ModifiedAt = DateTimeOffset.UtcNow
			};

			dbContext.Schemata.Add(schema);

			await dbContext.SaveChangesAsync();

			return CreateSchemaResult.Success(new(schema));
		}

		public async Task<UpdateSchemaResult> UpdateSchema(int id, DtoUpdateSchema dto, FragmentSchemaService fss, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, "IsAdmin");

			if (!authResult.Succeeded)
				return UpdateSchemaResult.AccessForbidden();

			Schema schema = dbContext.Schemata.Find(id);

			if (schema == null)
				return UpdateSchemaResult.SchemaNotFound();

			schema.Description = dto.Description;
			schema.Data = dto.Data;
			schema.ModifiedAt = DateTimeOffset.UtcNow;

			List<Schema> schemata = await dbContext.Schemata
				.Where(s => s.Id != id)
				.ToListAsync();

			schemata.Add(schema);

			XmlSchemaSet schemaSet;
			List<XSElement> fragments;

			try
			{
				(schemaSet, fragments) = FragmentSchemaService.ReadSchemata(schemata);
			}
			catch (XmlSchemaException ex)
			{
				return UpdateSchemaResult.BadSchemaParameters(ModelErrors.Create("Data", [string.Format("{0} Line {1}, position {2}.", ex.Message, ex.LineNumber, ex.LinePosition)]));
			}
			catch (System.Xml.XmlException ex)
			{
				return UpdateSchemaResult.BadSchemaParameters(ModelErrors.Create("Data", [ex.Message]));
			}
			catch (Exception ex)
			{
				return UpdateSchemaResult.BadSchemaParameters(ModelErrors.Create("Data", [ex.Message]));
			}

			await dbContext.SaveChangesAsync();

			fss.Reload(schemaSet, fragments);

			return UpdateSchemaResult.Success(new(schema));
		}

		public async Task<DeleteSchemaResult> DeleteSchema(int id, FragmentSchemaService fss, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, "IsAdmin");

			if (!authResult.Succeeded)
				return DeleteSchemaResult.AccessForbidden();

			Schema schema = dbContext.Schemata.Find(id);

			if (schema == null)
				return DeleteSchemaResult.SchemaNotFound();

			List<Schema> schemata = await dbContext.Schemata
				.Where(s => s.Id != id)
				.ToListAsync();

			XmlSchemaSet schemaSet;
			List<XSElement> fragments;

			try
			{
				(schemaSet, fragments) = FragmentSchemaService.ReadSchemata(schemata);
			}
			catch (Exception ex)
			{
				return DeleteSchemaResult.BadSchemaParameters(ModelErrors.Create("Id", [string.Format("This schema can't be deleted: {0}", ex.Message)]));
			}

			dbContext.Schemata.Remove(schema);

			await dbContext.SaveChangesAsync();

			fss.Reload(schemaSet, fragments);

			return DeleteSchemaResult.Success();
		}


	}
}