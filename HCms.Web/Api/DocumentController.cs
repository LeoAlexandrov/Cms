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
	public class DocumentController(ContentManagementService cms) : ControllerBase
	{
		private readonly ContentManagementService _cms = cms;

		[HttpGet("tree")]
		[Authorize]
		public async Task<IActionResult> Tree(CancellationToken ct)
		{
			var result = await _cms.DocumentTree(ct);
			return Ok(result);
		}

		[HttpGet("{id:int}")]
		[Authorize]
		public async Task<IActionResult> Get(int id, CancellationToken ct)
		{
			var result = await _cms.GetDocument(id, ct);

			if (result == null)
				return NotFound();

			return Ok(result);
		}

		[HttpPost]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> Post([Required] DtoCreateDocument dto, CancellationToken ct)
		{
			var result = await _cms.CreateDocument(dto, HttpContext.User, ct);

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
		public async Task<IActionResult> Put(int id, [Required] DtoUpdateDocument dto, CancellationToken ct)
		{
			var result = await _cms.UpdateDocument(id, dto, HttpContext.User, ct);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.Forbidden => Forbid(),
				ResultType.BadParameters => BadRequest(result.Errors),
				ResultType.Conflict => Conflict(result.Errors),
				_ => Ok(result.Value)
			};
		}

		[HttpDelete("{id:int}")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> Delete(int id, CancellationToken ct)
		{
			var result = await _cms.DeleteDocument(id, HttpContext.User, ct);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.Forbidden => Forbid(),
				ResultType.Success => Ok(),
				_ => BadRequest()
			};
		}

		[HttpGet("{id:int}/fragments")]
		[Authorize]
		public async Task<IActionResult> Fragments(int id, CancellationToken ct)
		{
			var result = await _cms.GetDocumentFragments(id, ct);
			return Ok(result);
		}


		[HttpPost("{id:int}/lock")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> SetLock(int id, [Required] DtoLockDocument dto, CancellationToken ct)
		{
			var result = await _cms.LockDocument(id, dto.LockState.Value, HttpContext.User, ct);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.Forbidden => Forbid(),
				ResultType.BadParameters => BadRequest(result.Errors),
				_ => Ok(result.Value)
			};
		}

		[HttpPost("{id:int}/parent")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> SetParent(int id, [Required] DtoSetParentDocument dto, CancellationToken ct)
		{
			var result = await _cms.SetParentDocument(id, dto.Parent, HttpContext.User, ct);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.Forbidden => Forbid(),
				ResultType.BadParameters => BadRequest(result.Errors),
				ResultType.Conflict => Conflict(result.Errors),
				_ => Ok(result.Value)
			};
		}

		[HttpPost("{id:int}/move")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> Move(int id, [Required] DtoMoveDocument dto, CancellationToken ct)
		{
			var result = await _cms.MoveDocument(id, dto.Increment.Value, HttpContext.User, ct);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.Forbidden => Forbid(),
				_ => Ok(result.Value)
			};
		}

		[HttpPost("{id:int}/copy")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> Copy(int id, CancellationToken ct)
		{
			var result = await _cms.CopyDocument(id, HttpContext.User, ct);

			return result.Type switch
			{
				ResultType.Forbidden => Forbid(),
				ResultType.BadParameters => BadRequest(result.Errors),
				_ => Ok(result.Value)
			};
		}

		[HttpGet("{id:int}/refs")]
		[Authorize]
		public async Task<IActionResult> References(int id, CancellationToken ct)
		{
			var result = await _cms.GetReferences(id, ct);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				_ => Ok(result.Value)
			};
		}

		[HttpGet("mediarefs")]
		[Authorize]
		public async Task<IActionResult> MediaReferers([FromQuery] string link, CancellationToken ct)
		{
			var result = await _cms.GetMediaReferers(link, ct);

			return result.Type switch
			{
				ResultType.BadParameters => BadRequest(result.Errors),
				_ => Ok(result.Value)
			};
		}

		[HttpGet("attributes/{id:int}")]
		[Authorize]
		public async Task<IActionResult> GetAttribute(int id, CancellationToken ct)
		{
			var result = await _cms.GetDocumentAttribute(id, ct);

			if (result == null)
				return NotFound();

			return Ok(result);
		}

		[HttpPost("attributes")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> PostAttribute([Required] DtoCreateDocumentAttribute dto, CancellationToken ct)
		{
			var result = await _cms.CreateAttribute(dto, HttpContext.User, ct);

			return result.Type switch
			{
				ResultType.Forbidden => Forbid(),
				ResultType.BadParameters => BadRequest(result.Errors),
				ResultType.Conflict => Conflict(result.Errors),
				_ => Ok(result.Value)
			};
		}

		[HttpPut("attributes/{id:int}")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> PutAttribute(int id, [Required] DtoUpdateDocumentAttribute dto, CancellationToken ct)
		{
			var result = await _cms.UpdateAttribute(id, dto, HttpContext.User, ct);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.Forbidden => Forbid(),
				ResultType.BadParameters => BadRequest(result.Errors),
				_ => Ok(result.Value)
			};
		}

		[HttpDelete("attributes/{id:int}")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> DeleteAttribute(int id, CancellationToken ct)
		{
			var result = await _cms.DeleteAttribute(id, HttpContext.User, ct);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.Forbidden => Forbid(),
				ResultType.Success => Ok(result.Value),
				_ => BadRequest()
			};
		}

	}
}
