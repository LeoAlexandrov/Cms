using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;

using HCms.ContentRepo;
using HCms.ViewModels;


namespace DemoSite.Services
{

	public class CmsContentService(IContentRepo repo, IAuthorizationService authorizationService)
	{
		const string EVENT_CREATE = "on_doc_create";
		const string EVENT_CHANGE = "on_doc_change";
		const string EVENT_UPDATE = "on_doc_update";
		const string EVENT_DELETE = "on_doc_delete";
		const string EVENT_XMLSCHEMA = "on_xmlschema_change";
		const string EVENT_ENABLED = "on_webhook_enable";
		const string EVENT_DISABLE = "on_webhook_disable";

		const string DEFAULT_ROOT = "home";

		readonly IContentRepo _repo = repo;
		readonly IAuthorizationService _authorizationService = authorizationService;

		public Document RequestedDocument { get; set; }
		public IContentRepo Repo { get => _repo; }

		public static string WebhookSecret { get; set; }


		public class Notification
		{
			public string Event { get; set; }
			public int AffectedDocument { get; set; }
			public string Secret { get; set; }
		}

		public enum AuthResult
		{
			Success,
			Unauthorized,
			Forbidden
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
			if (this.RequestedDocument == null || string.IsNullOrEmpty(this.RequestedDocument.AuthPolicies))
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


		public async Task<Document> GetDocument(string host, string path)
		{
			var (cmsRoot, cmsPath) = _repo.PathTransformer.Back(host, path);
			var doc = await _repo.GetDocument(cmsRoot ?? DEFAULT_ROOT, cmsPath, 0, true, false);

			RequestedDocument = doc;

			return doc;
		}

		public static async Task UpdateCache(Notification model, IMemoryCache cache, IContentRepo repo)
		{
			if (model.Secret != WebhookSecret)
				return;

			string cacheKey = null;

			switch (model.Event)
			{
				case EVENT_XMLSCHEMA:

					repo.ReloadSchemata();
					Console.WriteLine("*** Schema reloaded ***");
					break;

				case EVENT_UPDATE:

					var (root, path) = await repo.IdToPath(model.AffectedDocument);
					cacheKey = repo.PathTransformer.Forward(path, false, root);
					break;

				default:
					break;
			}


			if (!string.IsNullOrEmpty(cacheKey))
			{
				cache.Remove(cacheKey);
				Console.WriteLine("*** Entry '{0}' is removed ***", cacheKey);
			}
			else if (cache is MemoryCache memoryCache)
			{
				memoryCache.Clear();
				Console.WriteLine("*** Entire cache cleared ***");
			}

		}
	}

}