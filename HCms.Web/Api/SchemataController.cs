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
	public class SchemataController(SchemaManagementService sms) : ControllerBase

	{
		private readonly SchemaManagementService _sms = sms;


		[HttpGet("{id:int?}")]
		[Authorize]
		public async Task<IActionResult> Get(int? id, CancellationToken ct)
		{
			if (!id.HasValue)
			{
				var list = await _sms.Schemata();

				return Ok(list);
			}

			var result = await _sms.GetSchema(id.Value, ct);

			if (result == null)
				return NotFound();

			return Ok(result);
		}

		[HttpPost]
		[Authorize("IsAdmin")]
		[CsrAntiforgery]
		public async Task<IActionResult> Post([Required] DtoCreateSchema dto, CancellationToken ct)
		{
			var result = await _sms.CreateSchema(dto, HttpContext.User, ct);

			if (result.IsBadParameters)
				return BadRequest(result.Errors);

			return Ok(result.Value);
		}

		[HttpPut("{id:int}")]
		[Authorize("IsAdmin")]
		[CsrAntiforgery]
		public async Task<IActionResult> Put(int id, [Required] DtoUpdateSchema dto, CancellationToken ct)
		{
			var result = await _sms.UpdateSchema(id, dto, HttpContext.User, ct);

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
			var result = await _sms.DeleteSchema(id, HttpContext.User, ct);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.Forbidden => Forbid(),
				ResultType.BadParameters => BadRequest(result.Errors),
				_ => Ok(result.Value)
			};
		}

		[HttpPost("compile")]
		[Authorize("IsAdmin")]
		[CsrAntiforgery]
		public async Task<IActionResult> Compile()
		{
			var result = await _sms.CompileAndReload(HttpContext.User, HttpContext.RequestAborted);

			if (result.IsBadParameters)
				return BadRequest(result.Errors);

			return Ok();
		}

	}
}
