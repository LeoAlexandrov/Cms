using System;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;

using HCms.ContentRepo;
using HCms.Dto;
using HCms.ViewModels;


namespace DemoSite.Services
{

	public class CmsContentService(IContentRepo repo, IMemoryCache cache, IAuthorizationService authorizationService)
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

		ValueTask<AuthResult> AuthorizeEditor(ClaimsPrincipal user)
		{
			return ValueTask.FromResult(AuthResult.Forbidden);
		}

		public async Task<Document> GetDocument(string host, string path, int childPos, int takeChildren, ClaimsPrincipal user)
		{
			var (cmsRoot, cmsPath) = _repo.PathTransformer.Back(host, path);
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
					Console.WriteLine("*** Schema reloaded ***");
					break;

				case EVENT_DOC_UPDATE:

					string cacheKey;

					if (model.AffectedContent != null)
					{
						foreach (var con in model.AffectedContent)
						{
							cacheKey = _repo.PathTransformer.Forward(con.Root, con.Path, false);
							_cache.Remove($"dark-{cacheKey}");
							_cache.Remove($"light-{cacheKey}");
							_cache.Remove(cacheKey);
							Console.WriteLine($"*** Cache record '{cacheKey}' removed ***");
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
				Console.WriteLine("*** Entire cache cleared ***");
			}
		}

	}

}