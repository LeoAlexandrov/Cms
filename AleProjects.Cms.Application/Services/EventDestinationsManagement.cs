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
using AleProjects.Random;


namespace AleProjects.Cms.Application.Services
{
	public class EventDestinationsManagementService(CmsDbContext dbContext, IAuthorizationService authService, IEventNotifier notifier)
	{
		private readonly CmsDbContext _dbContext = dbContext;
		private readonly IAuthorizationService _authService = authService;
		private readonly IEventNotifier _notifier = notifier;


		public bool NoDestinations()
		{
			return !_dbContext.EventDestinations.Any(); ;
		}

		public async Task<DtoEventDestinationLiteResult[]> GetList()
		{
			var result = await _dbContext.EventDestinations
				.AsNoTracking()
				.OrderBy(d => d.Name)
				.Select(d => new DtoEventDestinationLiteResult(d))
				.ToArrayAsync();

			return result;
		}

		public async Task<DtoEventDestinationResult> GetById(int id, ClaimsPrincipal user)
		{
			var d = await _dbContext.EventDestinations.FindAsync(id);

			if (d == null)
				return null;

			DtoEventDestinationResult result = new(d, true);

			var authResult = await _authService.AuthorizeAsync(user, "IsAdmin");

			if (!authResult.Succeeded)
			{
				if (result.Webhook != null)
					result.Webhook.Secret = new string('*', 32);

				if (result.Redis != null)
					result.Redis.Password = new string('*', 16);

				if (result.RabbitMQ != null)
					result.RabbitMQ.Password = new string('*', 16);
			}

			return result;
		}

		public async Task<Result<DtoEventDestinationLiteResult>> CreateDestination(DtoCreateEventDestination dto, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, "IsAdmin");

			if (!authResult.Succeeded)
				return Result<DtoEventDestinationLiteResult>.Forbidden();

			if (dto.Type != "webhook" &&
				dto.Type != "redis" &&
				dto.Type != "rabbitmq")
				return Result<DtoEventDestinationLiteResult>.BadParameters("Type", [$"Destination of type '{dto.Type}' is not supported."]);

			object data = dto.Type switch
			{
				"webhook" => new WebhookDestination() { Endpoint = "https://localhost", Secret = RandomString.Create(32) },
				"redis" => new RedisDestination() { Endpoint = "localhost:6379", Channel = "hcms-channel" },
				"rabbitmq" => new RabbitMQDestination() { HostName = "localhost", Exchange = "hcms-exchange", ExchangeType = "fanout", RoutingKey = string.Empty },
				_ => null
			};

			EventDestination result = new()
			{
				Type = dto.Type,
				Name = dto.Name,
				Data = System.Text.Json.JsonSerializer.Serialize(data)
			};

			_dbContext.EventDestinations.Add(result);

			await _dbContext.SaveChangesAsync();

			return Result<DtoEventDestinationLiteResult>.Success(new(result));
		}

		public async Task<Result<DtoEventDestinationLiteResult>> UpdateDestination(int id, DtoUpdateEventDestination dto, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, "IsAdmin");

			if (!authResult.Succeeded)
				return Result<DtoEventDestinationLiteResult>.Forbidden();

			var result = await _dbContext.EventDestinations.FindAsync(id);

			if (result == null)
				return Result<DtoEventDestinationLiteResult>.NotFound();

			bool enabled = dto.Enabled.Value;
			bool becomesEnabled = !result.Enabled && enabled;
			bool becomesDisabled = result.Enabled && !enabled;
			object data;

			switch (result.Type)
			{
				case "webhook":

					if (dto.Webhook == null)
						return Result<DtoEventDestinationLiteResult>.BadParameters("Webhook", "Webhook data is required for this destination type.");

					var wData = System.Text.Json.JsonSerializer.Deserialize<WebhookDestination>(result.Data);

					wData.Endpoint = dto.Webhook.Endpoint;

					if (dto.Webhook.ResetSecret)
						wData.Secret = RandomString.Create(32);

					data = wData;
					break;


				case "redis":

					if (dto.Redis == null)
						return Result<DtoEventDestinationLiteResult>.BadParameters("Redis", "Redis data is required for this destination type.");

					data = new RedisDestination()
					{
						Endpoint = dto.Redis.Endpoint,
						Password = dto.Redis.Password,
						Channel = dto.Redis.Channel,
						User = dto.Redis.User
					};

					break;

				case "rabbitmq":

					if (dto.RabbitMQ == null)
						return Result<DtoEventDestinationLiteResult>.BadParameters("RabbitMQ", "RabbitMQ data is required for this destination type.");

					data = new RabbitMQDestination()
					{
						HostName = dto.RabbitMQ.HostName,
						User = dto.RabbitMQ.User,
						Password = dto.RabbitMQ.Password,
						Exchange = dto.RabbitMQ.Exchange,
						ExchangeType = dto.RabbitMQ.ExchangeType,
						RoutingKey = dto.RabbitMQ.RoutingKey
					};

					break;

				default:

					data = System.Text.Json.JsonSerializer.Deserialize<object>(result.Data);
					break;
			}

			result.Name = dto.Name;
			result.Enabled = enabled;
			result.TriggeringPath = dto.TriggeringPath;
			result.TriggeringPathAux = dto.TriggeringPathAux;
			result.Data = System.Text.Json.JsonSerializer.Serialize(data);

			if (string.IsNullOrEmpty(result.TriggeringPath))
			{
				result.TriggeringPath = result.TriggeringPathAux;
				result.TriggeringPathAux = null;
			}

			await _dbContext.SaveChangesAsync();

			if (becomesEnabled)
				await _notifier.Notify("on_destination_enable", id);
			else if (becomesDisabled)
				await _notifier.Notify("on_destination_disable", id);

			return Result<DtoEventDestinationLiteResult>.Success(new(result));
		}

		public async Task<Result<bool>> DeleteDestination(int id, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, "IsAdmin");

			if (!authResult.Succeeded)
				return Result<bool>.Forbidden();

			var d = await _dbContext.EventDestinations.FindAsync(id);

			if (d == null)
				return Result<bool>.NotFound();

			await _notifier.Notify("on_destination_disable", id);

			_dbContext.EventDestinations.Remove(d);

			await _dbContext.SaveChangesAsync();


			return Result<bool>.Success(true);
		}

	}
}