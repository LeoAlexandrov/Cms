using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

using HCms.Application.Services;
using HCms.Application.Dto;
using HCms.Web.Infrastructure.Filters;


namespace HCms.Web.Api
{

	[Route("api/v{version:apiVersion}/[controller]/{action}")]
	[ApiVersion("1.0")]
	[ApiController]
	public class MediaController(MediaManagementService mms) : ControllerBase
	{
		private readonly MediaManagementService _mms = mms;

		[HttpGet]
		[Authorize]
		public async Task<IActionResult> Folder([FromQuery] string link, CancellationToken ct)
		{
			var result = await _mms.Read(link, ct);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.BadParameters => BadRequest(result.Errors),
				_ => Ok(result.Value)
			};
		}

		[HttpGet]
		[Authorize]
		public async Task<IActionResult> Entry([FromQuery] string link, CancellationToken ct)
		{
			var result = await _mms.Get(link, ct);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.BadParameters => BadRequest(result.Errors),
				_ => result.Value.Content == null ?
						PhysicalFile(result.Value.FullPath, result.Value.MimeType) :
						File(result.Value.Content, result.Value.MimeType, result.Value.Size > 100 * 1024)
			};
		}

		[HttpGet]
		[Authorize]
		public async Task<IActionResult> Properties([FromQuery] string link, CancellationToken ct)
		{
			var result = await _mms.Properties(link, ct);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.BadParameters => BadRequest(result.Errors),
				_ => Ok(result.Value)
			};
		}

		[HttpGet]
		[Authorize]
		public async Task<IActionResult> Preview([FromQuery] string link, [FromQuery] int? size, CancellationToken ct)
		{
			var result = await _mms.Preview(link, size, ct);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.BadParameters => BadRequest(result.Errors),
				_ => PhysicalFile(result.Value.FullPath, result.Value.MimeType)
			};
		}

		[HttpPost]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> Upload()
		{
			CancellationToken ct = HttpContext.RequestAborted;
			MediaTypeHeaderValue cType = MediaTypeHeaderValue.Parse(Request.ContentType);
			string boundary = HeaderUtilities.RemoveQuotes(cType.Boundary).Value;
			MultipartReader reader = new(boundary, Request.Body);
			string destination = null;

			List<DtoMediaStorageEntry> uploaded = [];

			MultipartSection section = await reader.ReadNextSectionAsync(ct);

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

						var result = await _mms.Save(section.Body, contentDisposition.FileName.Value, destination, User, ct);

						if (!result.IsBadParameters)
							uploaded.Add(result.Value);
					}
					else if (contentDisposition.Name.Equals("destination"))
					{
						string dest = await section.ReadAsStringAsync(ct);
						destination = System.Web.HttpUtility.UrlDecode(dest ?? string.Empty);
					}
				}

				section = await reader.ReadNextSectionAsync(ct);
			}

			return Ok(uploaded);
		}

		[HttpGet]
		[Authorize]
		public async Task<IActionResult> Download([FromQuery] string link, CancellationToken ct)
		{
			var result = await _mms.Get(link, ct);

			return result.Type switch
			{
				ResultType.NotFound => NotFound(),
				ResultType.BadParameters => BadRequest(result.Errors),
				_ => result.Value.Content == null ?
						PhysicalFile(result.Value.FullPath, result.Value.MimeType, result.Value.DownloadName) :
						File(result.Value.Content, result.Value.MimeType, result.Value.DownloadName, result.Value.Size > 100 * 1024)
			};
		}


		[HttpPost]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> Folder([Required] DtoMediaStorageFolderCreate dto, CancellationToken ct)
		{
			var result = await _mms.CreateFolder(dto.Name, dto.Destination, User, ct);

			if (result.IsBadParameters)
				return BadRequest(result.Errors);

			return Ok(result.Value);
		}

		[HttpDelete]
		[Authorize("IsUser")]
		[CsrAntiforgery]
		public async Task<IActionResult> Entry([Required] DtoMediaStorageEntryDelete dto, CancellationToken ct)
		{
			if (dto.Links == null || dto.Links.Length == 0)
				return Ok();

			var result = await _mms.Delete(dto.Links, ct);

			if (result.IsBadParameters)
				return BadRequest(result.Errors);

			return Ok(result.Value);
		}

	}

}
