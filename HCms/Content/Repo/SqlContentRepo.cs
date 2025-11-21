using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using HCms.Content.Services;
using HCms.Content.ViewModels;
using HCms.Infrastructure.Data;


namespace HCms.Content.Repo
{
	/// <summary>
	/// CMS content repository querying database directly. Assumed to be a scoped service.
	/// </summary>
	public class SqlContentRepo : IContentRepo
	{
		readonly static FragmentSchemaRepo fsr = new();
		readonly static object lockObject = new();
		static int NeedsSchemataReload = 1;

		readonly CmsDbContext dbContext;
		readonly IPathMapper pathMapper;
		readonly ILoggerFactory loggerFactory;


		public SqlContentRepo(CmsDbContext dbContext, IPathMapper pathMapper, ILoggerFactory loggerFactory)
		{
			this.dbContext = dbContext;
			this.pathMapper = pathMapper;
			this.loggerFactory = loggerFactory;

			LoadFragmentSchemaService(dbContext);
		}


		#region private-functions

		static void LoadFragmentSchemaService(CmsDbContext dbContext)
		{
			if (NeedsSchemataReload > 0)
				lock (lockObject)
				{
					if (NeedsSchemataReload > 0)
					{
						fsr.Reload(dbContext);
						NeedsSchemataReload = 0;
					}
				}
		}

		#endregion


		/// <summary>
		/// Forces to reload schemata from the database when the next instance of SqlContentRepo is created.
		/// </summary>
		public void Reset()
		{
			NeedsSchemataReload = 1;
		}

		public async Task<Document> GetDocument(string root, string path, int childrenFromPos, int takeChildren, bool siblings, int[] allowedStatus, bool exactPathMatch)
		{
			var logger = loggerFactory.CreateLogger<ContentProvidingService>();
			var provider = new ContentProvidingService(dbContext, fsr, logger);
			var result = await provider.GetDocument(pathMapper, root, path, childrenFromPos, takeChildren, siblings, allowedStatus, exactPathMatch);

			return result;
		}

		public async Task<Document> GetDocument(int id, int childrenFromPos, int takeChildren, bool siblings, int[] allowedStatus)
		{
			var logger = loggerFactory.CreateLogger<ContentProvidingService>();
			var provider = new ContentProvidingService(dbContext, fsr, logger);
			var result = await provider.GetDocument(pathMapper, id, childrenFromPos, takeChildren, siblings, allowedStatus);

			return result;
		}

		public ValueTask<string> UserRole(string login)
		{
			var logger = loggerFactory.CreateLogger<ContentProvidingService>();
			var provider = new ContentProvidingService(dbContext, fsr, logger);

			return provider.UserRole(login);
		}

	}
}
