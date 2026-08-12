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
	public class UserController(UserManagementService ums) : ControllerBase
	{
		private readonly UserManagementService _ums = ums;

		[HttpGet("{id:int?}")]
		[Authorize]
		public async Task<IActionResult> Get(int? id, CancellationToken ct)
		{
			if (!id.HasValue)
			{
				var list = await _ums.GetList(HttpContext.User, ct);

				return Ok(list);
			}

			var result = await _ums.GetById(id.Value, HttpContext.User, ct);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.Forbidden => Forbid(),
				_ => Ok(result.Value)
			};
		}

		[HttpGet("bylogin/{login?}")]
		[Authorize]
		public async Task<IActionResult> GetByLogin(string login, CancellationToken ct)
		{
			if (string.IsNullOrEmpty(login))
				return NotFound();

			var result = await _ums.GetByLogin(login, HttpContext.User, ct);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.Forbidden => Forbid(),
				_ => Ok(result.Value)
			};
		}

		[HttpGet("roles")]
		[Authorize]
		public IActionResult Roles()
		{
			return Ok(_ums.UserRoles(HttpContext.User));
		}

		[HttpPost]
		[Authorize("IsAdmin")]
		[CsrAntiforgery]
		public async Task<IActionResult> Post([Required] DtoCreateUser dto, CancellationToken ct)
		{
			var result = await _ums.CreateUser(dto, HttpContext.User, ct);

			return result.Type switch
			{
				ResultType.Forbidden => Forbid(),
				ResultType.BadParameters => BadRequest(result.Errors),
				ResultType.Conflict => Conflict(result.Errors),
				_ => Ok(result.Value)
			};
		}

		[HttpPut("{id:int}")]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> Put(int id, [Required] DtoUpdateUser dto, CancellationToken ct)
		{
			var result = await _ums.UpdateUser(id, dto, HttpContext.User, ct);

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
		public async Task<IActionResult> Delete(int id, CancellationToken ct)
		{
			var result = await _ums.DeleteUser(id, HttpContext.User, ct);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.Forbidden => Forbid(),
				ResultType.BadParameters => BadRequest(result.Errors),
				_ => Ok(result.Value)
			};
		}

	}
}
