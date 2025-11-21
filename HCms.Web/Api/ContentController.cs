using System;
using System.Threading.Tasks;

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using HCms.Application.Dto;
using HCms.Content.Services;


namespace HCms.Web.Api
{

	[Route("api/v{version:apiVersion}/[controller]")]
	[ApiVersion("1.0")]
	[ApiController]
	public class ContentController(ContentProvidingService cps, IPathMapperFactory pathMapperFactory) : ControllerBase
	{
		private readonly IPathMapperFactory _pathMapperFactory = pathMapperFactory;
		private readonly ContentProvidingService _cps = cps;


		[HttpGet("doc/{id:int}")]
		[Authorize("IsConsumerApp")]
		public async Task<IActionResult> GetById(int id, [FromQuery] string pm, [FromQuery] int? cfp, [FromQuery] int? tc, [FromQuery] bool? sib, [FromQuery] int[] ast)
		{
			IPathMapper mapper = _pathMapperFactory.Get(pm);

			if (mapper == null)
				return BadRequest(new { name = nameof(pm), message = $"Path mapper '{pm}' is not found." });

			var result = await _cps.GetDocument(mapper, id, cfp ?? -1, tc ?? 1000, sib ?? true, ast ?? [1]);

			if (result == null)
				return NotFound();

			return Ok(result);
		}

		[HttpGet("doc/{root}")]
		[Authorize("IsConsumerApp")]
		public async Task<IActionResult> GetByPath(string root, [FromQuery] string path, [FromQuery] string pm, [FromQuery] int? cfp, [FromQuery] int? tc, [FromQuery] bool? sib, [FromQuery] int[] ast)
		{
			if (string.IsNullOrEmpty(root))
				return NotFound();

			IPathMapper mapper = _pathMapperFactory.Get(pm);

			if (mapper == null) 
				return BadRequest(new { name = nameof(pm), message = $"Path mapper '{pm}' is not found." });

			if (string.IsNullOrEmpty(path))
				path = "/";
			else if (!path.StartsWith('/'))
				path = '/' + path;

			var result = await _cps.GetDocument(mapper, root, path, cfp ?? -1, tc ?? 1000, sib ?? true, ast ?? [1], false);

			if (result == null)
				return NotFound();

			return Ok(result);
		}

		[HttpGet("children/{id:int}")]
		[Authorize("IsConsumerApp")]
		public async Task<IActionResult> GetChildren(int id, [FromQuery] string pm, [FromQuery] int? cfp, [FromQuery] int? tc, [FromQuery] bool? sib, [FromQuery] int[] ast)
		{
			IPathMapper mapper = _pathMapperFactory.Get(pm);

			if (mapper == null)
				return BadRequest(new { name = nameof(pm), message = $"Path mapper '{pm}' is not found." });

			var result = await _cps.GetChildren(mapper, id, cfp ?? -1, tc ?? 1000, ast ?? [1]);

			if (result == null)
				return NotFound();

			return Ok(result);
		}


		[HttpGet("role/{login}")]
		[Authorize("IsConsumerApp")]
		public async Task<IActionResult> GetRole(string login)
		{
			var role = await _cps.UserRole(login);

			if (role == null)
				return NotFound();

			return Ok(new UserRoleResponse() { Role = role });
		}

	}
}
