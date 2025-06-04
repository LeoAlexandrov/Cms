using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

using AleProjects.Cms.Application.Dto;
using AleProjects.Cms.Application.Services;
using AleProjects.Cms.Web.Infrastructure.Filters;



namespace AleProjects.Cms.Web.Api
{

	[Route("api/v{version:apiVersion}/[controller]")]
	[ApiVersion("1.0")]
	[ApiController]
	public class EventDestinationsController(EventDestinationsManagementService eds) : ControllerBase
	{
		private readonly EventDestinationsManagementService _eds = eds;

		[HttpGet("{id:int?}")]
		[Authorize]
		public async Task<IActionResult> Get(int? id)
		{
			if (!id.HasValue)
			{
				var list = await _eds.GetList();

				return Ok(list);
			}

			var result = await _eds.GetById(id.Value, this.HttpContext.User);

			if (result == null)
				return NotFound();

			return Ok(result);
		}

		[HttpPost]
		[Authorize("IsAdmin")]
		[CsrAntiforgery]
		public async Task<IActionResult> Post([Required] DtoCreateEventDestination dto)
		{
			var result = await _eds.CreateDestination(dto, this.HttpContext.User);

			return result.Type switch
			{
				ResultType.Forbidden => Forbid(),
				ResultType.BadParameters => BadRequest(result.Errors),
				ResultType.Conflict => Conflict(result.Errors),
				_ => Ok(result.Value)
			};
		}

		[HttpPut("{id:int}")]
		[Authorize]
		[CsrAntiforgery]
		public async Task<IActionResult> Put(int id, [Required] DtoUpdateEventDestination dto)
		{
			var result = await _eds.UpdateDestination(id, dto, this.HttpContext.User);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.Forbidden => Forbid(),
				ResultType.BadParameters => BadRequest(result.Errors),
				_ => Ok(result.Value)
			};
		}

		[HttpDelete("{id:int}")]
		[Authorize]
		[CsrAntiforgery]
		public async Task<IActionResult> Delete(int id)
		{
			var result = await _eds.DeleteDestination(id, this.HttpContext.User);

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
