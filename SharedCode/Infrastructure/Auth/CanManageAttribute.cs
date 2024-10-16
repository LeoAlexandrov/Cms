using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using AleProjects.Cms.Infrastructure.Data;


namespace AleProjects.Cms.Infrastructure.Auth
{

	public class CanManageAttributeRequirement : IAuthorizationRequirement { }


	public class CanManageAttributeHandler(CmsDbContext dbContext, IRoleClaimPolicies policies) : AuthorizationHandler<CanManageAttributeRequirement, int>
	{
		private readonly CmsDbContext _dbContext = dbContext;
		private readonly IRoleClaimPolicies _policies = policies;

		protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanManageAttributeRequirement requirement, int resource)
		{
			var doc = await _dbContext.Documents
				.Join(_dbContext.DocumentAttributes, d => d.Id, a => a.DocumentRef, (d, a) => new { d, a })
				.Where(da => da.a.Id == resource)
				.Select(da => da.d)
				.FirstOrDefaultAsync();

			if (doc == null || 
				string.IsNullOrEmpty(doc.EditorRoleRequired) || 
				_policies.ConformsPolicy(context.User, doc.EditorRoleRequired)) context.Succeed(requirement);
			
		}
	}

}