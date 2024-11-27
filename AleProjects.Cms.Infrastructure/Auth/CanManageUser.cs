using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

using AleProjects.Cms.Infrastructure.Data;
using System.Linq;
using System.Security.Claims;



namespace AleProjects.Cms.Infrastructure.Auth
{

	public class CanManageUserRequirement : IAuthorizationRequirement { }


	public class CanManageUserHandler(CmsDbContext dbContext, IRoleClaimPolicies policies) : AuthorizationHandler<CanManageUserRequirement, int>
	{
		private readonly CmsDbContext _dbContext = dbContext;
		private readonly IRoleClaimPolicies _policies = policies;

		protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanManageUserRequirement requirement, int resource)
		{
			var user = await _dbContext.Users.FindAsync(resource);

			if (user == null
				|| (_policies.Roles.Length > 0 && context.User.IsInRole(_policies.Roles[0])) // context.User is a developer
				|| (string.Compare(context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value, user.Login) == 0 && 
					user.IsEnabled) // context.User try to manages himself
				|| (_policies.Roles.Length > 1 && context.User.IsInRole(_policies.Roles[1]) && _policies.ConformsPolicy(context.User, user.Role))) // context.User is an admin
			{
				context.Succeed(requirement);
			}
		}

	}
}