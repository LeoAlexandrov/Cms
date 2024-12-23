using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

using AleProjects.Cms.Application.Dto;
using AleProjects.Cms.Application.Services;
using AleProjects.Cms.Domain.ValueObjects;
using AleProjects.Cms.Web.Infrastructure.Filters;



namespace AleProjects.Cms.Web.Api
{

	[Route("api/v{version:apiVersion}/[controller]")]
	[ApiVersion("1.0")]
	[ApiController]
	public class WebhooksController(WebhooksManagementService wms) : ControllerBase
	{
		private readonly WebhooksManagementService _wms = wms;

		[HttpGet("{id:int?}")]
		[Authorize]
		public async Task<IActionResult> Get(int? id)
		{
			if (!id.HasValue)
			{
				var list = await _wms.GetList();

				return Ok(list);
			}

			var result = await _wms.GetById(id.Value, this.HttpContext.User);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.Forbidden => Forbid(),
				_ => Ok(result.Value)
			};
		}

		[HttpPost]
		[Authorize("IsAdmin")]
		[CsrAntiforgery]
		public async Task<IActionResult> Post([Required] DtoCreateWebhook dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var result = await _wms.CreateWebhook(dto, this.HttpContext.User);

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
		public async Task<IActionResult> Put(int id, [Required] DtoUpdateWebhook dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var result = await _wms.UpdateWebhook(id, dto, this.HttpContext.User);

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
			var result = await _wms.DeleteWebhook(id, this.HttpContext.User);

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
