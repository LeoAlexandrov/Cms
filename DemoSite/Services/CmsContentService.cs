using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

using HCms.ContentRepo;
using HCms.ViewModels;


namespace DemoSite.Services
{

	public class CmsContentService(IContentRepo repo)
	{
		const string EVENT_CREATE = "on_doc_create";
		const string EVENT_CHANGE = "on_doc_change";
		const string EVENT_UPDATE = "on_doc_update";
		const string EVENT_DELETE = "on_doc_delete";
		const string EVENT_XMLSCHEMA = "on_xmlschema_change";
		const string EVENT_ENABLED = "on_webhook_enable";
		const string EVENT_DISABLE = "on_webhook_disable";

		readonly IContentRepo _repo = repo;

		public static string WebhookSecret { get; set; }

		public Document RequestedDocument { get; private set; }
		public IContentRepo Repo { get => _repo; }


		public class Notification
		{
			public string Event { get; set; }
			public int AffectedDocument { get; set; }
			public string Secret { get; set; }
		}


		public async Task<Document> GetDocument(string host, string path)
		{
			var (cmsRoot, cmsPath) = _repo.PathTransformer.Back(host, path);
			var doc = await _repo.GetDocument(cmsRoot ?? "home", cmsPath, 0, true);

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