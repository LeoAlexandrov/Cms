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
	public class AttributesController(ContentManagementService cms) : ControllerBase
	{
		private readonly ContentManagementService _cms = cms;

		[HttpGet("{id:int}")]
		[Authorize]
		public async Task<IActionResult> Get(int id)
		{
			var result = await _cms.GetAttribute(id);

			if (result == null)
				return NotFound();

			return Ok(result);
		}

		[HttpPost]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> Post([Required] DtoCreateDocumentAttribute dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var result = await _cms.CreateAttribute(dto, this.HttpContext.User);

			return result.Type switch
			{
				ResultType.BadParameters => BadRequest(result.Errors),
				ResultType.Conflict => Conflict(result.Errors),
				_ => Ok(result.Value)
			};
		}

		[HttpPut("{id:int}")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> Put(int id, [Required] DtoUpdateDocumentAttribute dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var result = await _cms.UpdateAttribute(id, dto, this.HttpContext.User);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.Forbidden => Forbid(),
				ResultType.BadParameters => BadRequest(result.Errors),
				_ => Ok(result.Value)
			};
		}

		[HttpDelete("{id:int}")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> Delete(int id)
		{
			var result = await _cms.DeleteAttribute(id, this.HttpContext.User);

			if (result.IsNotFound)
				return NotFound();

			if (!result.Ok)
				return BadRequest();

			return Ok();
		}

	}
}
