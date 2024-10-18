using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

using AleProjects.Cms.Infrastructure.Data;



namespace AleProjects.Cms.Infrastructure.Auth
{

	public class CanManageDocumentRequirement : IAuthorizationRequirement { }


	public class CanManageDocumentHandler(CmsDbContext dbContext, IRoleClaimPolicies policies) : AuthorizationHandler<CanManageDocumentRequirement, int>
	{
		private readonly CmsDbContext _dbContext = dbContext;
		private readonly IRoleClaimPolicies _policies = policies;

		protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanManageDocumentRequirement requirement, int resource)
		{
			var doc = await _dbContext.Documents.FindAsync(resource);

			if (doc == null || 
				string.IsNullOrEmpty(doc.EditorRoleRequired) || 
				_policies.ConformsPolicy(context.User, doc.EditorRoleRequired)) context.Succeed(requirement);
			
		}
	}

}