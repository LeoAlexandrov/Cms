using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

using AleProjects.Cms.Application.Dto;
using AleProjects.Cms.Application.Services;
using AleProjects.Cms.Web.Infrastructure.Filters;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;



namespace AleProjects.Cms.Web.Api
{

	[Route("api/v{version:apiVersion}/[controller]/{action}")]
	[ApiVersion("1.0")]
	[ApiController]
	public class MediaController(MediaManagementService mms) : ControllerBase
	{
		private readonly MediaManagementService _mms = mms;

		[HttpGet]
		[Authorize]
		public IActionResult Folder([FromQuery] string link)
		{
			var result = _mms.Read(link);

			if (result.NotFound)
				return NotFound();

			if (result.BadParameters)
				return BadRequest(result.Errors);

			return Ok(result.Result);
		}

		[HttpGet]
		[Authorize]
		public IActionResult Entry([FromQuery] string link)
		{
			var result = _mms.Get(link);

			if (result.NotFound)
				return NotFound();

			if (result.BadParameters)
				return BadRequest(result.Errors);

			return PhysicalFile(result.Result.FullPath, result.Result.MimeType);
		}

		[HttpGet]
		[Authorize]
		public async Task<IActionResult> Properties([FromQuery] string link)
		{
			var result = await _mms.Properties(link);

			if (result.NotFound)
				return NotFound();

			if (result.BadParameters)
				return BadRequest(result.Errors);

			return Ok(result.Result);
		}

		[HttpGet]
		[Authorize]
		public async Task<IActionResult> Preview([FromQuery] string link, [FromQuery] int? size)
		{
			var result = await _mms.Preview(link, size);

			if (result.NotFound)
				return NotFound();

			if (result.BadParameters)
				return BadRequest(result.Errors);

			return PhysicalFile(result.Result.FullPath, result.Result.MimeType);
		}

		[CsrAntiforgery]
		[HttpPost]
		[Authorize("IsUser")]
		public async Task<IActionResult> Upload()
		{
			MediaTypeHeaderValue cType = MediaTypeHeaderValue.Parse(Request.ContentType);
			string boundary = HeaderUtilities.RemoveQuotes(cType.Boundary).Value;
			MultipartReader reader = new(boundary, Request.Body);
			string destination = null;

			List<DtoMediaStorageEntry> uploaded = [];

			MultipartSection section = await reader.ReadNextSectionAsync();

			while (section != null)
			{
				var hasContentDisposition = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);

				if (hasContentDisposition && contentDisposition.DispositionType.Equals("form-data"))
				{
					if (!string.IsNullOrEmpty(contentDisposition.FileName.Value))
					{

						if (destination == null)
						{
							ModelState.AddModelError("Destination", "Must be specified before any file data.");
							return BadRequest(ModelState);
						}

						var result = await _mms.Save(section.Body, contentDisposition.FileName.Value, destination, this.User);

						if (!result.BadParameters)
							uploaded.Add(result.Result);
					}
					else if (contentDisposition.Name.Equals("destination"))
					{
						string dest = await section.ReadAsStringAsync();
						destination = System.Web.HttpUtility.UrlDecode(dest ?? "");
					}
				}

				section = await reader.ReadNextSectionAsync();
			}

			return Ok(uploaded);
		}

		[CsrAntiforgery]
		[HttpPost]
		[Authorize("IsUser")]
		public async Task<IActionResult> Folder([Required] DtoMediaStorageFolderCreate dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var result = await _mms.CreateFolder(dto.Name, dto.Destination, this.User);

			if (result.BadParameters)
				return BadRequest(result.Errors);

			return Ok(result.Result);
		}

		[CsrAntiforgery]
		[HttpDelete]
		[Authorize("IsUser")]
		public async Task<IActionResult> Entry([Required] DtoMediaStorageEntryDelete dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			if (dto.Links == null || dto.Links.Length == 0)
				return Ok();

			var result = await _mms.Delete(dto.Links);

			if (result.BadParameters)
				return BadRequest(result.Errors);

			return Ok(result.Result);
		}

	}

}
