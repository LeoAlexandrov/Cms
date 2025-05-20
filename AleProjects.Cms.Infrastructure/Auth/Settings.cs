using System;
using System.Collections.Generic;


namespace AleProjects.Cms.Infrastructure.Auth
{
	public class OAuthAppCredentials
	{
		public string ClientId { get; set; }
		public string ClientSecret { get; set; }
		public string Key { get; set; }
	}



	public class CloudflareTTCredentials
	{
		public string SiteKey { get; set; }
		public string SecretKey { get; set; }
	}



	public class AuthSettings
	{
		public string SecurityKey { get; set; }
		public string JwtIssuer { get; set; }
		public string JwtAudience { get; set; }
		public bool DemoMode { get; set; }
		public string DefaultDemoModeRole { get; set; }
		public string[] OrderedRoles { get; set; }
		public Dictionary<string, string[]> RoleClaimPolicies { get; set; }
		public OAuthAppCredentials Google { get; set; }
		public OAuthAppCredentials Microsoft { get; set; }
		public OAuthAppCredentials Github { get; set; }
		public OAuthAppCredentials StackOverflow { get; set; }
		public CloudflareTTCredentials CloudflareTT { get; set; }
	}
}
