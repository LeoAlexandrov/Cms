using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using AleProjects.Cms.Application.Dto;
using AleProjects.Cms.Domain.Entities;
using AleProjects.Cms.Domain.ValueObjects;
using AleProjects.Cms.Infrastructure.Data;
using AleProjects.Cms.Infrastructure.Notification;


namespace AleProjects.Cms.Application.Services
{
	public class WebhooksManagementService(CmsDbContext dbContext, IAuthorizationService authService, IWebhookNotifier notifier)
	{
		private readonly CmsDbContext _dbContext = dbContext;
		private readonly IAuthorizationService _authService = authService;
		private readonly IWebhookNotifier _notifier = notifier;


		public bool NoWebhooks()
		{
			return !_dbContext.Webhooks.Any(); ;
		}

		public async Task<DtoWebhookLiteResult[]> GetList()
		{
			var result = await _dbContext.Webhooks
				.AsNoTracking()
				.OrderBy(w => w.Endpoint)
				.Select(w => new DtoWebhookLiteResult(w))
				.ToArrayAsync();

			return result;
		}

		public async Task<Result<DtoWebhookResult>> GetById(int id, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, "IsAdmin");

			if (!authResult.Succeeded)
				return Result<DtoWebhookResult>.Forbidden();

			var w = await _dbContext.Webhooks.FindAsync(id);

			if (w == null)
				return Result<DtoWebhookResult>.NotFound();

			return Result<DtoWebhookResult>.Success(new(w, true));
		}

		public async Task<Result<DtoWebhookLiteResult>> CreateWebhook(DtoCreateWebhook dto, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, "IsAdmin");

			if (!authResult.Succeeded)
				return Result<DtoWebhookLiteResult>.Forbidden();

			Webhook result = new()
			{
				Endpoint = dto.Endpoint,
				RootDocument = dto.RootDocument,
				Secret = AleProjects.Cms.RandomString.Create(32),
				Enabled = true
			};

			_dbContext.Webhooks.Add(result);

			await _dbContext.SaveChangesAsync();

			return Result<DtoWebhookLiteResult>.Success(new(result));
		}

		public async Task<Result<DtoWebhookLiteResult>> UpdateWebhook(int id, DtoUpdateWebhook dto, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, "IsAdmin");

			if (!authResult.Succeeded)
				return Result<DtoWebhookLiteResult>.Forbidden();

			var result = await _dbContext.Webhooks.FindAsync(id);

			if (result == null)
				return Result<DtoWebhookLiteResult>.NotFound();

			bool enabled = !result.Enabled && dto.Enabled;

			result.Endpoint = dto.Endpoint;
			result.RootDocument = dto.RootDocument;
			result.Enabled = dto.Enabled;

			if (dto.ResetSecret)
				result.Secret = RandomString.Create(32);

			await _dbContext.SaveChangesAsync();

			if (enabled)
				await _notifier.Notify("on_webhook_enable", 0);

			return Result<DtoWebhookLiteResult>.Success(new(result));
		}

		public async Task<Result<bool>> DeleteWebhook(int id, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, "IsAdmin");

			if (!authResult.Succeeded)
				return Result<bool>.Forbidden();

			var w = await _dbContext.Webhooks.FindAsync(id);

			if (w == null)
				return Result<bool>.NotFound();

			_dbContext.Webhooks.Remove(w);

			await _dbContext.SaveChangesAsync();

			return Result<bool>.Success(true);
		}

	}
}