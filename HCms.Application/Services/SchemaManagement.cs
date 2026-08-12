using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;

using HCms.Application.Dto;
using HCms.Domain.Entities;
using HCms.Infrastructure.Notification;
using HCms.Infrastructure.Data;


namespace HCms.Application.Services
{

	public class SchemaManagementService(CmsDbContext dbContext, FragmentSchemaRepo fsr, IAuthorizationService authService, IEventNotifier notifier)
	{
		private readonly CmsDbContext dbContext = dbContext;
		private readonly FragmentSchemaRepo fsr = fsr;
		private readonly IAuthorizationService _authService = authService;
		private readonly IEventNotifier _notifier = notifier;


		public Task<DtoSchemaResult[]> Schemata()
		{
			return FragmentSchemaRepo.List(dbContext, s => new DtoSchemaResult(s));
		}

		public async Task<DtoSchemaResult> GetSchema(int id, CancellationToken ct)
		{
			Schema schema = await FragmentSchemaRepo.GetSchema(dbContext, id, ct);

			return new(schema);
		}

		public async Task<Result<DtoSchemaResult>> CreateSchema(DtoCreateSchema dto, ClaimsPrincipal user, CancellationToken ct)
		{
			var authResult = await _authService.AuthorizeAsync(user, "IsAdmin");

			if (!authResult.Succeeded)
				return Result<DtoSchemaResult>.Forbidden();

			var result = await FragmentSchemaRepo.CreateSchema(dbContext, dto.Description, ct);

			return Result<DtoSchemaResult>.Success(new(result));
		}

		public async Task<Result<DtoSchemaResult>> UpdateSchema(int id, DtoUpdateSchema dto, ClaimsPrincipal user, CancellationToken ct)
		{
			var authResult = await _authService.AuthorizeAsync(user, "IsAdmin");

			if (!authResult.Succeeded)
				return Result<DtoSchemaResult>.Forbidden();

			var (schema, error)  = await fsr.UpdateSchema(dbContext, id, dto.Description, dto.Data, dto.OnlySave.Value, ct);

			if (schema != null)
			{
				await _notifier.Notify("on_xmlschema_change", 0, CancellationToken.None);

				return Result<DtoSchemaResult>.Success(new(schema));
			}

			return string.IsNullOrEmpty(error) ?
				Result<DtoSchemaResult>.NotFound() :
				Result<DtoSchemaResult>.BadParameters("Data", [error]);
		}

		public async Task<Result<bool>> DeleteSchema(int id, ClaimsPrincipal user, CancellationToken ct)
		{
			var authResult = await _authService.AuthorizeAsync(user, "IsAdmin");

			if (!authResult.Succeeded)
				return Result<bool>.Forbidden();

			var (ok, error) = await fsr.DeleteSchema(dbContext, id, ct);

			if (ok)
			{
				await _notifier.Notify("on_xmlschema_change", 0, CancellationToken.None);

				return Result<bool>.Success(true);
			}

			return string.IsNullOrEmpty(error) ?
				Result<bool>.NotFound() :
				Result<bool>.BadParameters("Id", [$"This schema can't be deleted: {error}"]);
		}

		public async Task<Result<bool>> CompileAndReload(ClaimsPrincipal user, CancellationToken ct)
		{
			var authResult = await _authService.AuthorizeAsync(user, "IsAdmin");

			if (!authResult.Succeeded)
				return Result<bool>.Forbidden();

			var (ok, error) = await fsr.CompileAndReload(dbContext, ct);

			if (ok)
			{
				await _notifier.Notify("on_xmlschema_change", 0, CancellationToken.None);

				return Result<bool>.Success(true);
			}

			return Result<bool>.BadParameters("Data", [$"Compilation error: {error}"]);
		}
	}
}