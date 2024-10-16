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



	public class RoleClaimPolicies : IRoleClaimPolicies
	{
		protected readonly Dictionary<string, string[]> _policies;
		protected readonly string[] _roles;

		public string[] Roles { get => _roles; }

		public RoleClaimPolicies(IConfiguration config)
		{
			var orderedRoles = config.GetSection("Auth:OrderedRoles")?.AsEnumerable();

			_roles = orderedRoles != null ?
				orderedRoles
					.OrderByDescending(r => int.TryParse(r.Key[(r.Key.LastIndexOf(':') + 1)..], out int idx) ? idx : -1)
					.Select(s => s.Value)
					.Where(s => !string.IsNullOrEmpty(s))
					.ToArray() :
				[];

			_policies = Create(config);
		}

		public bool ConformsPolicy(ClaimsPrincipal user, string requiredRole)
		{
			string role = user.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

			return !string.IsNullOrEmpty(role) && Array.IndexOf(Roles, requiredRole) >= Array.IndexOf(Roles, role);
		}


		private static Dictionary<string, string[]> Create(IConfiguration config)
		{
			var cfgPolicies = config.GetSection("Auth:RoleClaimPolicies")?.GetChildren();
			var policies = new Dictionary<string, string[]>();

			if (cfgPolicies != null)
				foreach (var policy in cfgPolicies)
					policies[policy.Key] = policy
						.AsEnumerable()
						.Select(s => s.Value)
						.Where(s => !string.IsNullOrEmpty(s))
						.ToArray();

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
