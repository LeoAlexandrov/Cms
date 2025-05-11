using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Entities = AleProjects.Cms.Domain.Entities;
using AleProjects.Cms.Infrastructure.Data;

using HCms.Routing;
using HCms.ViewModels;


namespace HCms.ContentRepo
{

	/// <summary>
	/// CMS content repository querying content via Rest or Grpc. Not implemented yet.
	/// </summary>
	public class RemoteContentRepo : ContentRepo, IContentRepo
	{

		public void ReloadSchemata()
		{
			throw new NotImplementedException();
		}

		public Task<Document[]> Children(int docId, int childrenFromPos, int take, int[] allowedStatus)
		{
			throw new NotImplementedException();
		}

		public Task<Document> GetDocument(string root, string path, int childrenFromPos, int takeChildren, bool siblings, int[] allowedStatus, bool exactPathMatch)
		{
			throw new NotImplementedException();
		}

		public Task<Document> GetDocument(int id, int childrenFromPos, int takeChildren, bool siblings, int[] allowedStatus)
		{
			throw new NotImplementedException();
		}

		public Task<(string, string)> IdToPath(int docId)
		{
			throw new NotImplementedException();
		}
	}

}