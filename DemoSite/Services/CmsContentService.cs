using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using HCms.ContentRepo;
using HCms.Dto;
using HCms.ViewModels;


namespace DemoSite.Services
{

	public class CmsContentService(
		IContentRepo repo, 
		IMemoryCache cache, 
		IAuthorizationService authorizationService,
		ILogger<CmsContentService> logger)
	{
		public const string EVENT_DOC_CREATE = "on_doc_create";
		public const string EVENT_DOC_CHANGE = "on_doc_change";
		public const string EVENT_DOC_UPDATE = "on_doc_update";
		public const string EVENT_DOC_DELETE = "on_doc_delete";
		public const string EVENT_MEDIA_CREATE = "on_media_create";
		public const string EVENT_MEDIA_DELETE = "on_media_delete";
		public const string EVENT_XMLSCHEMA = "on_xmlschema_change";
		public const string EVENT_ENABLE = "on_destination_enable";
		public const string EVENT_DISABLE = "on_destination_disable";

		const string DEFAULT_ROOT = "home";

		public readonly static ConcurrentDictionary<string, AwaitedResult> AwaitedResults = new();

		readonly IContentRepo _repo = repo;
		readonly IMemoryCache _cache = cache;
		readonly IAuthorizationService _authorizationService = authorizationService;
		readonly ILogger<CmsContentService> _logger = logger;

		public Document RequestedDocument { get; set; }
		public IContentRepo Repo { get => _repo; }


		public enum AuthResult
		{
			Success,
			Unauthorized,
			Forbidden
		}

		public class AwaitedResult
		{
			public CancellationTokenSource Cts { get; set; }
			public byte[] Body { get; set; }
		}


		static Task<bool> Authorize(ClaimsPrincipal user, string[] policies, IAuthorizationService authorizationService, bool all)
		{
			Task<bool> taskChain = Task.FromResult(all);

			foreach (var policy in policies)
			{
				taskChain = taskChain
					.ContinueWith(async previousTask =>
					{
						if (previousTask.Result ^ all)
							return !all;

						bool success;

						try
						{
							var result = await authorizationService.AuthorizeAsync(user, policy.Trim());
							success = result.Succeeded;
						}
						catch
						{
							success = false;
						}

						return success;
					})
					.Unwrap();
			}

			return taskChain;
		}

		public async ValueTask<AuthResult> Authorize(ClaimsPrincipal user)
		{
			if (this.RequestedDocument == null || !this.RequestedDocument.AuthRequired)
				return AuthResult.Success;

			if (user.Identity?.IsAuthenticated != true)
				return AuthResult.Unauthorized;

			string policies = this.RequestedDocument.AuthPolicies;
			int commaIdx = policies.IndexOf(',');
			int semicolonIdx = policies.IndexOf(';');
			bool result;

			if (commaIdx == -1 && semicolonIdx == -1)
				result = await Authorize(user, [policies], _authorizationService, true);
			else if (commaIdx == -1 || commaIdx > semicolonIdx)
				result = await Authorize(user, policies.Split(';'), _authorizationService, false);
			else if (semicolonIdx == -1 || commaIdx < semicolonIdx)
				result = await Authorize(user, policies.Split(','), _authorizationService, true);
			else
				result = false;

			return result ? AuthResult.Success : AuthResult.Forbidden;
		}

		async ValueTask<AuthResult> AuthorizeEditor(ClaimsPrincipal user)
		{
			if (!user.Identity.IsAuthenticated)
				return AuthResult.Unauthorized;

			string login = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

			if (string.IsNullOrEmpty(login))
				return AuthResult.Forbidden;

			string role = await _repo.UserRole(login);

			return string.IsNullOrEmpty(role) ? AuthResult.Forbidden : AuthResult.Success;
		}

		public async Task<Document> GetDocument(string cmsRoot, string cmsPath, int childPos, int takeChildren, ClaimsPrincipal user)
		{
			int[] allowedStatus = await AuthorizeEditor(user) == AuthResult.Success ? [1, 2] : [1];
			var doc = await _repo.GetDocument(cmsRoot ?? DEFAULT_ROOT, cmsPath, childPos, takeChildren, true, allowedStatus, false);

			RequestedDocument = doc;

			return doc;
		}

		public void UpdateCache(EventPayload model)
		{
			switch (model.Event)
			{
				case EVENT_XMLSCHEMA:

					_repo.ReloadSchemata();
					_logger.LogInformation("Schemata have been reloaded");
					break;

				case EVENT_DOC_UPDATE:

					if (model.AffectedContent != null)
					{
						string path;

						foreach (var con in model.AffectedContent)
						{
							path = string.IsNullOrEmpty(con.Path) ? "/" : con.Path;
							_cache.Remove($"{con.Root}-dark-{path}");
							_cache.Remove($"{con.Root}-light-{path}");

							_logger.LogInformation("Cache record '{path}' has been removed", path);
						}

						return;
					}

					break;

				default:
					break;
			}


			if (_cache is MemoryCache memoryCache)
			{
				memoryCache.Clear();

				_logger.LogInformation("Entire cache has been cleared");
			}
		}

	}

}