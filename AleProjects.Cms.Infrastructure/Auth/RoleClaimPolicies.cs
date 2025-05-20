using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;


namespace AleProjects.Cms.Infrastructure.Auth
{
	public interface IRoleClaimPolicies
	{
		string[] Roles { get; }
		bool ConformsPolicy(ClaimsPrincipal user, string requiredRole);
	}



	public class RoleClaimPolicies(IOptions<AuthSettings> settings) : IRoleClaimPolicies
	{
		protected readonly Dictionary<string, string[]> _policies = settings.Value.RoleClaimPolicies;
		protected readonly string[] _roles = settings.Value.OrderedRoles.Reverse().ToArray();

		public string[] Roles { get => _roles; }

		public bool ConformsPolicy(ClaimsPrincipal user, string requiredRole)
		{
			string role = user.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

			return !string.IsNullOrEmpty(role) && Array.IndexOf(Roles, requiredRole) >= Array.IndexOf(Roles, role);
		}

		public static void CreatePolicies(IConfiguration config, AuthorizationOptions options)
		{
			var policies = config.GetSection("Auth:RoleClaimPolicies").Get<Dictionary<string, string[]>>();

			foreach (var policy in policies)
				options.AddPolicy(
					policy.Key,
					policyBuilder => policyBuilder
						.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, "ApiKey")
						.RequireAuthenticatedUser()
						.RequireClaim(ClaimsIdentity.DefaultRoleClaimType, policy.Value));
		}
	}
}
