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
	public class DocumentsController(ContentManagementService cms) : ControllerBase
	{
		private readonly ContentManagementService _cms = cms;

		[HttpGet("tree")]
		[Authorize]
		public async Task<IActionResult> Tree()
		{
			var result = await _cms.DocumentsTree();
			return Ok(result);
		}

		[HttpGet("{id:int}")]
		[Authorize]
		public async Task<IActionResult> Get(int id)
		{
			var result = await _cms.GetDocument(id);

			if (result == null)
				return NotFound();

			return Ok(result);
		}

		[HttpPost]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> Post([Required] DtoCreateDocument dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var result = await _cms.CreateDocument(dto, this.HttpContext.User);

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
		public async Task<IActionResult> Put(int id, [Required] DtoUpdateDocument dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var result = await _cms.UpdateDocument(id, dto, this.HttpContext.User);

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
		public async Task<IActionResult> Delete(int id)
		{
			var result = await _cms.DeleteDocument(id, this.HttpContext.User);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.Success => Ok(),
				_ => BadRequest()
			};
		}

		[HttpPost("{id:int}/lock")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> SetLock(int id, [Required] DtoLockDocument dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var result = await _cms.LockDocument(id, dto.LockState, this.HttpContext.User);

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
		public async Task<IActionResult> SetParent(int id, [Required] DtoSetParentDocument dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var result = await _cms.SetParentDocument(id, dto.Parent, this.HttpContext.User);

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
		public async Task<IActionResult> Move(int id, [Required] DtoMoveDocument dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var result = await _cms.MoveDocument(id, dto.Increment, this.HttpContext.User);

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
		public async Task<IActionResult> Copy(int id)
		{
			var result = await _cms.CopyDocument(id, this.HttpContext.User);

			return result.Type switch
			{
				ResultType.Forbidden => Forbid(),
				ResultType.BadParameters => BadRequest(result.Errors),
				_ => Ok(result.Value)
			};
		}

		[HttpGet("attributes/{id:int}")]
		[Authorize]
		public async Task<IActionResult> GetAttribute(int id)
		{
			var result = await _cms.GetDocumentAttribute(id);

			if (result == null)
				return NotFound();

			return Ok(result);
		}

		[HttpPost("attributes")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> PostAttribute([Required] DtoCreateDocumentAttribute dto)
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

		[HttpPut("attributes/{id:int}")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> PutAttribute(int id, [Required] DtoUpdateDocumentAttribute dto)
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

		[HttpDelete("attributes/{id:int}")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> DeleteAttribute(int id)
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
