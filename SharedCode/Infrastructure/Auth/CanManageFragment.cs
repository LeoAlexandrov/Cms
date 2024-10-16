using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using AleProjects.Cms.Infrastructure.Data;


namespace AleProjects.Cms.Infrastructure.Auth
{

	public class CanManageFragmentRequirement : IAuthorizationRequirement { }


	public class CanManageFragmentHandler(CmsDbContext dbContext, IRoleClaimPolicies policies) : AuthorizationHandler<CanManageFragmentRequirement, int>
	{
		private readonly CmsDbContext _dbContext = dbContext;
		private readonly IRoleClaimPolicies _policies = policies;

		protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanManageFragmentRequirement requirement, int resource)
		{
			var doc = await _dbContext.Documents
				.Join(_dbContext.FragmentLinks, d => d.Id, f => f.DocumentRef, (d, f) => new { d, f })
				.Where(dn => dn.f.Id == resource)
				.Select(dn => dn.d)
				.FirstOrDefaultAsync();

			if (doc == null || 
				string.IsNullOrEmpty(doc.EditorRoleRequired) || 
				_policies.ConformsPolicy(context.User, doc.EditorRoleRequired)) context.Succeed(requirement);
			
		}
	}

}