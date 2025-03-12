using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;


namespace AleProjects.Cms.Infrastructure.Auth
{
	public interface IRoleClaimPolicies
	{
		string[] Roles { get; }
		bool ConformsPolicy(ClaimsPrincipal user, string requiredRole);
	}



	public class RoleClaimPolicies(IConfiguration config) : IRoleClaimPolicies
	{
		protected readonly Dictionary<string, string[]> _policies = Create(config);
		protected readonly string[] _roles = config.GetSection("Auth:OrderedRoles")?.Get<string[]>().Reverse().ToArray() ?? [];

		public string[] Roles { get => _roles; }

		public bool ConformsPolicy(ClaimsPrincipal user, string requiredRole)
		{
			string role = user.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

			return !string.IsNullOrEmpty(role) && Array.IndexOf(Roles, requiredRole) >= Array.IndexOf(Roles, role);
		}


		private static Dictionary<string, string[]> Create(IConfiguration config)
		{
			var policies = config.GetSection("Auth:RoleClaimPolicies").Get<Dictionary<string, string[]>>();

			return policies;
		}

		public static void CreatePolicies(IConfiguration config, AuthorizationOptions options)
		{
			var policies = Create(config);

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
