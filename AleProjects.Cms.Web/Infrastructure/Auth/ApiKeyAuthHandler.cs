using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

using AleProjects.Cms.Application.Dto;
using AleProjects.Cms.Application.Services;
using AleProjects.Cms.Infrastructure.Auth;


namespace AleProjects.Cms.Web.Infrastructure.Auth
{

	public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
	{
		const string APIKEY_HEADER = "APIKey";

		readonly UserManagementService _ums;
		readonly AuthSettings _settings;

		public ApiKeyAuthenticationHandler(
			IOptionsMonitor<AuthenticationSchemeOptions> options,
			ILoggerFactory logger,
			UrlEncoder encoder,
			UserManagementService ums,
			IOptions<AuthSettings> settings) : base(options, logger, encoder)
		{
			_ums = ums;
			_settings = settings.Value;
		}

		protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			var authHeader = Context.Request.Headers[APIKEY_HEADER];

			if (authHeader == StringValues.Empty)
				return AuthenticateResult.NoResult();

			string token = authHeader[0];
			var result = Result<DtoUserResult>.NotFound();

			if (_settings.ApiKeys != null)
				foreach (var key in _settings.ApiKeys)
					if (key.Key == token)
					{
						result = Result<DtoUserResult>.Success(new DtoUserResult()
						{
							Name = key.Name,
							Role = key.Role,
							IsEnabled = true
						});

						break;
					}
			

			if (result.IsNotFound)
				result = await _ums.GetByApiKey(token);

			if (result.IsNotFound || !result.Value.IsEnabled)
				return AuthenticateResult.NoResult();

			List<Claim> claims = [
				new(ClaimTypes.Name, result.Value.Name),
				new(ClaimTypes.Role, result.Value.Role)
			];

			if (!string.IsNullOrEmpty(result.Value.Locale))
				claims.Add(new("locale", result.Value.Locale));

			var identity = new ClaimsIdentity(claims, this.Scheme.Name);
			var principal = new ClaimsPrincipal(identity);
			var ticket = new AuthenticationTicket(principal, this.Scheme.Name);

			return AuthenticateResult.Success(ticket);
		}
	}

}
