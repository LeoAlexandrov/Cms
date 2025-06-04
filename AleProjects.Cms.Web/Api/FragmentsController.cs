using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Asp.Versioning;

using AleProjects.Cms.Application.Dto;
using AleProjects.Cms.Application.Services;
using AleProjects.Cms.Infrastructure.Data;
using AleProjects.Cms.Web.Infrastructure.Filters;


namespace AleProjects.Cms.Web.Api
{

	[Route("api/v{version:apiVersion}/[controller]")]
	[ApiVersion("1.0")]
	[ApiController]
	public class FragmentsController(ContentManagementService cms, FragmentSchemaRepo schemaRepo, IHtmlLocalizer<SharedResources> sharedLocalizer) : ControllerBase
	{
		private readonly ContentManagementService _cms = cms;
		private readonly FragmentSchemaRepo _schemaRepo = schemaRepo;
		private readonly IHtmlLocalizer<SharedResources> _sharedLocalizer = sharedLocalizer;

		[HttpGet("shared")]
		[Authorize]
		public async Task<IActionResult> Shared()
		{
			var result = await _cms.SharedFragments();
			return Ok(result);
		}

		[HttpGet("creationstuff")]
		[Authorize]
		public async Task<IActionResult> CreationStuff()
		{
			var result = await _cms.FragmentCreationStuff(_sharedLocalizer.GetString("Language"));

			return Ok(result);
		}

		[HttpGet("{id:int}")]
		[Authorize]
		public async Task<IActionResult> Get(int id)
		{
			var result = await _cms.GetFragmentByLink(id, _sharedLocalizer.GetString("Language"));

			if (result.IsNotFound)
				return NotFound();

			return Ok(result.Value);
		}

		[HttpPost]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> Post([Required] DtoCreateFragment dto)
		{
			var result = await _cms.CreateFragment(dto, this.HttpContext.User);

			return result.Type switch
			{
				ResultType.Forbidden => Forbid(),
				ResultType.BadParameters => BadRequest(result.Errors),
				_ => Ok(result.Value)
			};
		}

		[HttpPut("{id:int}")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> Put(int id, [Required] DtoFullFragment dto)
		{
			var result = await _cms.UpdateFragmentByLink(id, dto, this.HttpContext.User);

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
			var result = await _cms.DeleteFragmentByLink(id, this.HttpContext.User);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.Forbidden => Forbid(),
				ResultType.Success => Ok(result.Value),
				_ =>  BadRequest()
			};
		}

		[HttpPost("{id:int}/move")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> Move(int id, [Required] DtoMoveFragment dto)
		{
			var result = await _cms.MoveFragment(id, dto.Increment.Value, this.HttpContext.User);

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
			var result = await _cms.CopyFragment(id, this.HttpContext.User);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.Forbidden => Forbid(),
				ResultType.BadParameters => BadRequest(result.Errors),
				_ => Ok(result.Value)
			};
		}

		[HttpPost("{id:int}/container")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> SetContainer(int id, [Required] DtoSetFragmentContainer dto)
		{
			var result = await _cms.SetFragmentContainer(id, dto.LinkId.Value, this.HttpContext.User);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.Forbidden => Forbid(),
				ResultType.BadParameters => BadRequest(result.Errors),
				_ => Ok(result.Value)
			};
		}


		[HttpPost("reloadschema")]
		[Authorize("IsAdmin")]
		[CsrAntiforgery]
		public IActionResult ReloadSchema([FromServices] CmsDbContext dbContext)
		{
			if (_schemaRepo.Reload(dbContext))
				return Ok();

			return StatusCode(500);
		}

		[HttpGet("newelement")]
		public IActionResult NewElement([FromQuery] string path)
		{
			var result = ContentManagementService.NewFragmentElementValue(path, _sharedLocalizer.GetString("Language"), _schemaRepo.Index);

			if (result == null)
				return BadRequest();

			return Ok(result);
		}

		[HttpGet("attributes/{id:int}")]
		[Authorize]
		public async Task<IActionResult> GetAttribute(int id)
		{
			var result = await _cms.GetFragmentAttribute(id);

			if (result == null)
				return NotFound();

			return Ok(result);
		}

		[HttpPost("attributes")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> PostAttribute([Required] DtoCreateFragmentAttribute dto)
		{
			var result = await _cms.CreateAttribute(dto, this.HttpContext.User);

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
		public async Task<IActionResult> PutAttribute(int id, [Required] DtoUpdateFragmentAttribute dto)
		{
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
		public async Task<IActionResult> DeleteAttribute(int id, [Required] [FromQuery] int documentRef)
		{
			var result = await _cms.DeleteAttribute(id, documentRef, this.HttpContext.User);

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
