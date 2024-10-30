using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

using AleProjects.Cms.Application.Services;


namespace AleProjects.Cms.Web.Infrastructure.Auth
{

	public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
	{
		private const string APIKEY_HEADER = "APIKey";

		private readonly IUserManagementService _ums;

		public ApiKeyAuthenticationHandler(
			IOptionsMonitor<AuthenticationSchemeOptions> options,
			ILoggerFactory logger,
			UrlEncoder encoder,
			IUserManagementService ums) : base(options, logger, encoder)
		{
			_ums = ums;
		}

		protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			var authHeader = Context.Request.Headers[APIKEY_HEADER];

			if (authHeader == StringValues.Empty)
				return AuthenticateResult.NoResult();

			string token = authHeader[0];

			var ur = await _ums.GetByApiKey(token);

			if (ur.IsNotFound || !ur.Value.IsEnabled)
				return AuthenticateResult.NoResult();

			List<Claim> claims = [
				new(ClaimTypes.Name, ur.Value.Name),
				new(ClaimTypes.Role, ur.Value.Role)
			];

			if (!string.IsNullOrEmpty(ur.Value.Locale))
				claims.Add(new("locale", ur.Value.Locale));

			var identity = new ClaimsIdentity(claims, this.Scheme.Name);
			var principal = new ClaimsPrincipal(identity);
			var ticket = new AuthenticationTicket(principal, this.Scheme.Name);

			return AuthenticateResult.Success(ticket);
		}
	}

}
