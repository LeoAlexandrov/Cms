using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

using AleProjects.Cms.Application.Dto;
using AleProjects.Cms.Domain.Entities;
using AleProjects.Cms.Domain.ValueObjects;
using AleProjects.Cms.Infrastructure.Data;


namespace AleProjects.Cms.Application.Services
{

	public class SchemaManagementService(CmsDbContext dbContext, FragmentSchemaRepo fsr, IAuthorizationService authService)
	{
		private readonly CmsDbContext dbContext = dbContext;
		private readonly FragmentSchemaRepo fsr = fsr;
		private readonly IAuthorizationService _authService = authService;


		public Task<DtoSchemaResult[]> Schemata()
		{
			return FragmentSchemaRepo.List(dbContext, s => new DtoSchemaResult(s));
		}

		public async Task<DtoSchemaResult> GetSchema(int id)
		{
			Schema schema = await FragmentSchemaRepo.GetSchema(dbContext, id);

			return new(schema);
		}

		public async Task<Result<DtoSchemaResult>> CreateSchema(DtoCreateSchema dto, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, "IsAdmin");

			if (!authResult.Succeeded)
				return Result<DtoSchemaResult>.Forbidden();

			var result = await FragmentSchemaRepo.CreateSchema(dbContext, dto.Description);

			return Result<DtoSchemaResult>.Success(new(result));
		}

		public async Task<Result<DtoSchemaResult>> UpdateSchema(int id, DtoUpdateSchema dto, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, "IsAdmin");

			if (!authResult.Succeeded)
				return Result<DtoSchemaResult>.Forbidden();

			var result = await fsr.UpdateSchema(dbContext, id, dto.Description, dto.Data, dto.OnlySave);

			if (!result.Ok)
				return Result<DtoSchemaResult>.From(result);

			return Result<DtoSchemaResult>.Success(new(result.Value));
		}

		public async Task<Result<bool>> DeleteSchema(int id, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, "IsAdmin");

			if (!authResult.Succeeded)
				return Result<bool>.Forbidden();

			return await fsr.DeleteSchema(dbContext, id);
		}

		public async Task<Result<bool>> CompileAndReload(ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, "IsAdmin");

			if (!authResult.Succeeded)
				return Result<bool>.Forbidden();

			return await fsr.CompileAndReload(dbContext);
		}
	}
}