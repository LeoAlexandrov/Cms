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
	public class UsersController(UserManagementService ums) : ControllerBase

	{
		private readonly UserManagementService _ums = ums;

		[HttpGet("{id:int?}")]
		[Authorize]
		public async Task<IActionResult> Get(int? id)
		{
			if (!id.HasValue)
			{
				var list = await _ums.GetList(this.HttpContext.User);

				return Ok(list);
			}

			var result = await _ums.GetById(id.Value, this.HttpContext.User);

			if (result.NotFound)
				return NotFound();

			if (result.Forbidden)
				return Forbid();

			return Ok(result.Result);
		}

		[HttpGet("roles")]
		[Authorize]
		public IActionResult Roles()
		{
			return Ok(_ums.UserRoles(this.HttpContext.User));
		}

		[HttpPost]
		[Authorize("IsAdmin")]
		[CsrAntiforgery]
		public async Task<IActionResult> Post([Required] DtoCreateUser dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var result = await _ums.CreateUser(dto, this.HttpContext.User);

			if (result.Forbidden)
				return Forbid();

			if (result.BadParameters)
				return BadRequest(result.Errors);

			if (result.Conflict)
				return Conflict(result.Errors);

			return Ok(result.Result);
		}

		[HttpPut("{id:int}")]
		[Authorize]
		[CsrAntiforgery]
		public async Task<IActionResult> Put(int id, [Required] DtoUpdateUser dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var result = await _ums.UpdateUser(id, dto, this.HttpContext.User);

			if (result.NotFound)
				return NotFound();

			if (result.Forbidden)
				return Forbid();

			if (result.BadParameters)
				return BadRequest(result.Errors);

			return Ok(result.Result);
		}

		[HttpDelete("{id:int}")]
		[Authorize]
		[CsrAntiforgery]
		public async Task<IActionResult> Delete(int id)
		{
			var result = await _ums.DeleteUser(id, this.HttpContext.User);

			if (result.NotFound)
				return NotFound();

			if (result.Forbidden)
				return Forbid();

			if (result.BadParameters)
				return BadRequest(result.Errors);

			return Ok(result.Result);
		}


	}
}
