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

			if (result.BadParameters)
				return BadRequest(result.Errors);

			if (result.Conflict)
				return Conflict(result.Errors);

			return Ok(result.Result);
		}

		[HttpPut("{id:int}")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> Put(int id, [Required] DtoUpdateDocument dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var result = await _cms.UpdateDocument(id, dto, this.HttpContext.User);

			if (result.NotFound)
				return NotFound();

			if (result.Forbidden)
				return Forbid();

			if (result.BadParameters)
				return BadRequest(result.Errors);

			if (result.Conflict)
				return Conflict(result.Errors);

			return Ok(result.Result);
		}

		[HttpDelete("{id:int}")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> Delete(int id)
		{
			var result = await _cms.DeleteDocument(id, this.HttpContext.User);

			if (result.NotFound)
				return NotFound();

			if (!result.Ok)
				return BadRequest();

			return Ok();
		}

		[HttpPost("{id:int}/lock")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> SetLock(int id, [Required] DtoLockDocument dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var result = await _cms.LockDocument(id, dto.LockState, this.HttpContext.User);

			if (result.NotFound)
				return NotFound();

			if (result.Forbidden)
				return Forbid();

			if (result.BadParameters)
				return BadRequest(result.Errors);

			return Ok(result.Result);
		}

		[HttpPost("{id:int}/parent")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> SetParent(int id, [Required] DtoSetParentDocument dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var result = await _cms.SetParentDocument(id, dto.Parent, this.HttpContext.User);

			if (result.NotFound)
				return NotFound();

			if (result.Forbidden)
				return Forbid();

			if (result.BadParameters)
				return BadRequest(result.Errors);

			if (result.Conflict)
				return BadRequest(result.Errors);

			return Ok(result.Result);
		}

		[HttpPost("{id:int}/move")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> Move(int id, [Required] DtoMoveDocument dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var result = await _cms.MoveDocument(id, dto.Increment, this.HttpContext.User);

			if (result.NotFound)
				return NotFound();

			if (result.Forbidden)
				return Forbid();

			return Ok(result.Result);
		}

		[HttpPost("{id:int}/copy")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> Copy(int id)
		{
			var result = await _cms.CopyDocument(id, this.HttpContext.User);

			if (result.Forbidden)
				return Forbid();

			if (result.BadParameters)
				return BadRequest(result.Errors);

			return Ok(result.Result);
		}
	}
}
