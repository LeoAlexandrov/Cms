using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using HCms.Application.Services;
using HCms.Application.Dto;
using HCms.Web.Infrastructure.Filters;


namespace HCms.Web.Api
{

	[Route("api/v{version:apiVersion}/[controller]")]
	[ApiVersion("1.0")]
	[ApiController]
	public class EventDestinationController(EventDestinationManagementService eds) : ControllerBase
	{
		private readonly EventDestinationManagementService _eds = eds;

		[HttpGet("{id:int?}")]
		[Authorize]
		public async Task<IActionResult> Get(int? id, CancellationToken ct)
		{
			if (!id.HasValue)
			{
				var list = await _eds.GetList(ct);

				return Ok(list);
			}

			var result = await _eds.GetById(id.Value, HttpContext.User, ct);

			if (result == null)
				return NotFound();

			return Ok(result);
		}

		[HttpPost]
		[Authorize("IsAdmin")]
		[CsrAntiforgery]
		public async Task<IActionResult> Post([Required] DtoCreateEventDestination dto, CancellationToken ct)
		{
			var result = await _eds.CreateDestination(dto, HttpContext.User, ct);

			return result.Type switch
			{
				ResultType.Forbidden => Forbid(),
				ResultType.BadParameters => BadRequest(result.Errors),
				ResultType.Conflict => Conflict(result.Errors),
				_ => Ok(result.Value)
			};
		}

		[HttpPut("{id:int}")]
		[Authorize("IsAdmin")]
		[CsrAntiforgery]
		public async Task<IActionResult> Put(int id, [Required] DtoUpdateEventDestination dto, CancellationToken ct)
		{
			var result = await _eds.UpdateDestination(id, dto, HttpContext.User, ct);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.Forbidden => Forbid(),
				ResultType.BadParameters => BadRequest(result.Errors),
				_ => Ok(result.Value)
			};
		}

		[HttpDelete("{id:int}")]
		[Authorize("IsAdmin")]
		[CsrAntiforgery]
		public async Task<IActionResult> Delete(int id, CancellationToken ct)
		{
			var result = await _eds.DeleteDestination(id, HttpContext.User, ct);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.Forbidden => Forbid(),
				ResultType.BadParameters => BadRequest(result.Errors),
				_ => Ok()
			};
		}

	}
}
