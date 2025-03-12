using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using Google.Apis.Auth;
using MessagePack;

using AleProjects.Cms.Domain.Entities;
using AleProjects.Cms.Infrastructure.Data;
using AleProjects.Random;



namespace AleProjects.Cms.Infrastructure.Auth
{

	public enum LoginStatus
	{
		Success,
		InvalidToken,
		InvalidPayload,
		Forbidden,
		Expiration,
		InternalError
	}



	[MessagePackObject(keyAsPropertyName: true)]
	public class UserLogin
	{
		public int UserId { get; set; }
		
		public DateTime LoginTime { get; set; } = DateTime.UtcNow;
		
		public string PreviousRefreshToken { get; set; }

		[MessagePack.IgnoreMember]
		public LoginStatus Status { get; set; }

		[MessagePack.IgnoreMember]
		public string Jwt { get; set; }

		[MessagePack.IgnoreMember]
		public string Refresh { get; set; }

		[MessagePack.IgnoreMember]
		public string Locale { get; set; }

		public static UserLogin WithStatus(LoginStatus status) => new() { Status = status };

		public static UserLogin Success(int userId, DateTime loginTime, string jwt, string refresh, string locale) => 
			new() { UserId = userId, LoginTime = loginTime, Status = LoginStatus.Success, Jwt = jwt, Refresh = refresh, Locale = locale };
	}



	public class GoogleAuthPayload
	{
		[Required]
		public string ClientId { get; set; }

		[Required]
		public string Credential { get; set; }

		public string Select_By { get; set; }

		[Required]
		public string G_Csrf_Token { get; set; }
	}



	public class SignInHandler(CmsDbContext context, IConfiguration configuration, IDistributedCache cache, IHttpClientFactory httpClientFactory)
	{
#if DEBUG
		const int JWT_EXPIRES_IN = 60;
#else
		const int JWT_EXPIRES_IN = 300;
#endif
		const int REFRESH_EXPIRES_IN = 3600;

		const string MS_ACCESS_TOKEN = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
		const string MS_USER = "https://graph.microsoft.com/v1.0/me";
		const string GITHUB_ACCESS_TOKEN = "https://github.com/login/oauth/access_token";
		const string GITHUB_USER = "https://api.github.com/user";
		const string STACKOVERFLOW_ACCESS_TOKEN = "https://stackoverflow.com/oauth/access_token/json";
		const string STACKOVERFLOW_USER = "https://api.stackexchange.com//2.3/me?order=desc&sort=reputation&site=stackoverflow&access_token={0}&key={1}";

		static readonly SemaphoreSlim _semaphore = new(1, 1);

		readonly CmsDbContext _dbContext = context;
		readonly IConfiguration _configuration = configuration;
		readonly IDistributedCache _cache = cache;
		readonly IHttpClientFactory _httpClientFactory = httpClientFactory;


		#region local

		class MicrosoftTokenResponse
		{
			[JsonPropertyName("token_type")]
			public string TokenType { get; set; }

			[JsonPropertyName("scope")]
			public string Scope { get; set; }

			[JsonPropertyName("expires_in")]
			public int ExpiresIn { get; set; }

			[JsonPropertyName("ext_expires_in")]
			public int ExtExpiresIn { get; set; }

			[JsonPropertyName("access_token")]
			public string AccessToken { get; set; }
		}

		class MicrosoftUser
		{
			[JsonPropertyName("userPrincipalName")]
			public string UserPrincipalName { get; set; }

			[JsonPropertyName("displayName")]
			public string DisplayName { get; set; }

			[JsonPropertyName("surname")]
			public string Surname { get; set; }

			[JsonPropertyName("givenName")]
			public string GivenName { get; set; }

			[JsonPropertyName("preferredLanguage")]
			public string PreferredLanguage { get; set; }

			[JsonPropertyName("mail")]
			public string Mail { get; set; }
		}

		class GithubCodeExchange
		{
			[JsonPropertyName("code")]
			public string Code { get; set; }

			[JsonPropertyName("client_id")]
			public string ClientId { get; set; }

			[JsonPropertyName("client_secret")]
			public string ClientSecret { get; set; }
		}

		class GithubTokenResponse
		{
			[JsonPropertyName("token_type")]
			public string TokenType { get; set; }

			[JsonPropertyName("scope")]
			public string Scope { get; set; }

			[JsonPropertyName("access_token")]
			public string AccessToken { get; set; }
		}

		class GithubUser
		{
			[JsonPropertyName("login")]
			public string Login { get; set; }

			[JsonPropertyName("name")]
			public string Name { get; set; }

			[JsonPropertyName("email")]
			public string Email { get; set; }

			[JsonPropertyName("avatar_url")]
			public string Avatar { get; set; }
		}

		class StackOverflowTokenResponse
		{
			[JsonPropertyName("access_token")]
			public string AccessToken { get; set; }
		}

		class StackOverflowUserItem
		{
			[JsonPropertyName("account_id")]
			public long AccountId { get; set; }

			[JsonPropertyName("user_id")]
			public long UserId { get; set; }

			[JsonPropertyName("display_name")]
			public string DisplayName { get; set; }

			[JsonPropertyName("profile_image")]
			public string ProfileImage { get; set; }
		}

		class StackOverflowUser
		{
			[JsonPropertyName("items")]
			public StackOverflowUserItem[] Items { get; set; }
		}

		#endregion

		private string CreateJwt(User user)
		{
			var claims = new List<Claim>()
			{
				new(ClaimTypes.NameIdentifier, user.Login),
				new(ClaimTypes.Name, user.Name),
				new(ClaimTypes.Role, user.Role),
			};

			if (!string.IsNullOrEmpty(user.Email))
				claims.Add(new Claim(ClaimTypes.Email, user.Email));

			if (!string.IsNullOrEmpty(user.Locale))
				claims.Add(new Claim("locale", user.Locale));

			if (!string.IsNullOrEmpty(user.Avatar))
				claims.Add(new Claim("avt", user.Avatar));

			DateTime now = DateTime.UtcNow;
			byte[] key = Encoding.ASCII.GetBytes(_configuration.GetValue<string>("Auth:SecurityKey"));

			JwtSecurityToken token = new(
				issuer: _configuration.GetValue<string>("Auth:JwtIssuer"),
				audience: _configuration.GetValue<string>("Auth:JwtAudience"),
				notBefore: now,
				claims: [.. claims],
				expires: now.Add(TimeSpan.FromSeconds(JWT_EXPIRES_IN)),
				signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256));

			string jwt = new JwtSecurityTokenHandler().WriteToken(token);

			return jwt;
		}


		public async Task<UserLogin> Google(GoogleAuthPayload gPayload)
		{
			GoogleJsonWebSignature.Payload payload;

			try
			{
				payload = await GoogleJsonWebSignature.ValidateAsync(gPayload.Credential);
			}
			catch
			{
				return UserLogin.WithStatus(LoginStatus.InvalidToken);
			}

			bool demoMode = _configuration.GetValue<bool>("Auth:DemoMode");

			var user = _dbContext.Users.FirstOrDefault(u => u.Login == payload.Email);

			if (user == null && demoMode)
			{
				string defaultDemoModeRole = _configuration.GetValue<string>("Auth:DefaultDemoModeRole");

				_dbContext.Users.Add(user = new() { Login = payload.Email, Role = defaultDemoModeRole, IsEnabled = true, IsDemo = true });
			}

			if (user == null || !user.IsEnabled || (user.IsDemo && !demoMode))
				return UserLogin.WithStatus(LoginStatus.Forbidden);

			if (string.IsNullOrEmpty(user.Name))
				user.Name = payload.Name ?? payload.Email;

			if (string.IsNullOrEmpty(user.Email))
				user.Email = payload.Email;

			if (string.IsNullOrEmpty(user.Avatar))
				user.Avatar = payload.Picture;

			if (string.IsNullOrEmpty(user.Role))
				user.Role = "User";

			if (string.IsNullOrEmpty(user.Locale))
				user.Locale = payload.Locale;

			user.LastSignIn = DateTimeOffset.UtcNow;

			await _dbContext.SaveChangesAsync();

			UserLogin login = UserLogin.Success(user.Id, user.LastSignIn.Value.UtcDateTime, CreateJwt(user), RandomString.Create(32), user.Locale);
			byte[] bLogin = MessagePackSerializer.Serialize(login);

			_cache.Set(login.Refresh, bLogin, new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(REFRESH_EXPIRES_IN) });

			return login;
		}

		public async Task<UserLogin> Microsoft(string code, string host)
		{
			HttpClient client = _httpClientFactory.CreateClient();

			MicrosoftTokenResponse msToken;

			using (HttpRequestMessage request = new() { Method = HttpMethod.Post, RequestUri = new Uri(MS_ACCESS_TOKEN) })
			{
				string body = string.Format("code={0}&client_id={1}&client_secret={2}&scope=User.Read&grant_type=authorization_code&redirect_uri={3}://{4}/auth/microsoft",
					code,
					_configuration.GetValue<string>("Auth:Microsoft:ClientId"),
					_configuration.GetValue<string>("Auth:Microsoft:ClientSecret"),
					 "https",
					 host);

				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				request.Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

				using HttpResponseMessage response = await client.SendAsync(request);

				response.EnsureSuccessStatusCode();

				msToken = await response.Content.ReadFromJsonAsync<MicrosoftTokenResponse>();
			}

			MicrosoftUser msUser;

			using (HttpRequestMessage request = new() { Method = HttpMethod.Get, RequestUri = new Uri(MS_USER) })
			{
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", msToken.AccessToken);

				using HttpResponseMessage response = await client.SendAsync(request);

				response.EnsureSuccessStatusCode();

				msUser = await response.Content.ReadFromJsonAsync<MicrosoftUser>();
			}

			string upn = msUser.UserPrincipalName;
			bool demoMode = _configuration.GetValue<bool>("Auth:DemoMode");

			var user = _dbContext.Users.FirstOrDefault(u => u.Login == upn);

			if (user == null && demoMode)
			{
				string defaultDemoModeRole = _configuration.GetValue<string>("Auth:DefaultDemoModeRole");

				_dbContext.Users.Add(user = new() { Login = upn, Role = defaultDemoModeRole, IsEnabled = true, IsDemo = true });
			}

			if (user == null || !user.IsEnabled || (user.IsDemo && !demoMode))
				return UserLogin.WithStatus(LoginStatus.Forbidden);

			if (string.IsNullOrEmpty(user.Name))
			{
				string name = msUser.DisplayName;

				if (string.IsNullOrEmpty(name))
				{
					var nameParts = new string[] {
							msUser.GivenName,
							msUser.Surname
						};

					name = string.Join(' ', nameParts.Where(n => !string.IsNullOrEmpty(n)));

					if (string.IsNullOrEmpty(name))
						name = upn;
				}

				user.Name = name;
			}

			if (string.IsNullOrEmpty(user.Email))
				user.Email = upn;

			if (string.IsNullOrEmpty(user.Role))
				user.Role = "User";

			if (string.IsNullOrEmpty(user.Locale))
				user.Locale = msUser.PreferredLanguage;

			user.LastSignIn = DateTimeOffset.UtcNow;

			await _dbContext.SaveChangesAsync();

			UserLogin login = UserLogin.Success(user.Id, user.LastSignIn.Value.UtcDateTime, CreateJwt(user), RandomString.Create(32), user.Locale);
			byte[] bLogin = MessagePackSerializer.Serialize(login);

			_cache.Set(login.Refresh, bLogin, new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(REFRESH_EXPIRES_IN) });

			return login;
		}

		public async Task<UserLogin> Github(string code, string userAgent)
		{
			HttpClient client = _httpClientFactory.CreateClient();

			GithubCodeExchange codeExchange = new()
			{
				Code = code,
				ClientId = _configuration.GetValue<string>("Auth:Github:ClientId"),
				ClientSecret = _configuration.GetValue<string>("Auth:Github:ClientSecret"),
			};

			GithubTokenResponse ghTtoken;

			using (HttpRequestMessage request = new() { Method = HttpMethod.Post, RequestUri = new Uri(GITHUB_ACCESS_TOKEN), Content = JsonContent.Create(codeExchange) })
			{
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

				using HttpResponseMessage response = await client.SendAsync(request);

				response.EnsureSuccessStatusCode();

				ghTtoken = await response.Content.ReadFromJsonAsync<GithubTokenResponse>();
			}

			GithubUser ghUser;

			using (HttpRequestMessage request = new() { Method = HttpMethod.Get, RequestUri = new Uri(GITHUB_USER) })
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ghTtoken.AccessToken);
				request.Headers.Add("User-Agent", userAgent);

				using HttpResponseMessage response = await client.SendAsync(request);

				response.EnsureSuccessStatusCode();

				ghUser = await response.Content.ReadFromJsonAsync<GithubUser>();
			}

			bool demoMode = _configuration.GetValue<bool>("Auth:DemoMode");

			var user = _dbContext.Users.FirstOrDefault(u => u.Login == ghUser.Login);

			if (user == null && demoMode)
			{
				string defaultDemoModeRole = _configuration.GetValue<string>("Auth:DefaultDemoModeRole");

				_dbContext.Users.Add(user = new() { Login = ghUser.Login, Role = defaultDemoModeRole, IsEnabled = true, IsDemo = true });
			}

			if (user == null || !user.IsEnabled || (user.IsDemo && !demoMode))
				return UserLogin.WithStatus(LoginStatus.Forbidden);

			if (string.IsNullOrEmpty(user.Name))
				user.Name = ghUser.Name ?? ghUser.Login;

			if (string.IsNullOrEmpty(user.Email))
				user.Email = ghUser.Email;

			if (string.IsNullOrEmpty(user.Avatar))
				user.Avatar = ghUser.Avatar;

			if (string.IsNullOrEmpty(user.Role))
				user.Role = "User";

			user.LastSignIn = DateTimeOffset.UtcNow;

			await _dbContext.SaveChangesAsync();

			UserLogin login = UserLogin.Success(user.Id, user.LastSignIn.Value.UtcDateTime, CreateJwt(user), RandomString.Create(32), user.Locale);
			byte[] bLogin = MessagePackSerializer.Serialize(login);

			_cache.Set(login.Refresh, bLogin, new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(REFRESH_EXPIRES_IN) });

			return login;
		}

		public async Task<UserLogin> Stackoverflow(string code, string redurectUri, string userAgent)
		{
			HttpClient client = _httpClientFactory.CreateClient();

			Dictionary<string, string> codeExchange = new()
			{
				{ "code" , code },
				{ "client_id", _configuration.GetValue<string>("Auth:StackOverflow:ClientId") },
				{ "client_secret", _configuration.GetValue<string>("Auth:StackOverflow:ClientSecret") },
				{ "redirect_uri", redurectUri }
			};

			StackOverflowTokenResponse soToken;

			using (HttpRequestMessage request = new() { Method = HttpMethod.Post, RequestUri = new Uri(STACKOVERFLOW_ACCESS_TOKEN), Content = new FormUrlEncodedContent(codeExchange) })
			{
				request.Headers.Add("User-Agent", userAgent);

				using HttpResponseMessage response = await client.SendAsync(request);

				response.EnsureSuccessStatusCode();

				soToken = await response.Content.ReadFromJsonAsync<StackOverflowTokenResponse>();
			}

			StackOverflowUser soUser;
			string key = _configuration.GetValue<string>("Auth:StackOverflow:Key");

			using (HttpRequestMessage request = new() { Method = HttpMethod.Get, RequestUri = new Uri(string.Format(STACKOVERFLOW_USER, soToken.AccessToken, key)) })
			{
				request.Headers.Add("User-Agent", userAgent);

				using HttpResponseMessage response = await client.SendAsync(request);

				response.EnsureSuccessStatusCode();

				soUser = await response.Content.ReadFromJsonAsync<StackOverflowUser>();
			}

			string uid = soUser.Items[0].UserId.ToString();
			bool demoMode = _configuration.GetValue<bool>("Auth:DemoMode");

			var user = _dbContext.Users.FirstOrDefault(u => u.Login == uid);

			if (user == null && demoMode)
			{
				string defaultDemoModeRole = _configuration.GetValue<string>("Auth:DefaultDemoModeRole");

				_dbContext.Users.Add(user = new() { Login = uid, Role = defaultDemoModeRole, IsEnabled = true, IsDemo = true });
			}

			if (user == null || !user.IsEnabled || (user.IsDemo && !demoMode))
				return UserLogin.WithStatus(LoginStatus.Forbidden);

			if (string.IsNullOrEmpty(user.Name))
				user.Name = soUser.Items[0].DisplayName ?? uid;

			if (string.IsNullOrEmpty(user.Avatar))
				user.Avatar = soUser.Items[0].ProfileImage;

			if (string.IsNullOrEmpty(user.Role))
				user.Role = "User";

			user.LastSignIn = DateTimeOffset.UtcNow;

			await _dbContext.SaveChangesAsync();

			UserLogin login = UserLogin.Success(user.Id, user.LastSignIn.Value.UtcDateTime, CreateJwt(user), RandomString.Create(32), user.Locale);
			byte[] bLogin = MessagePackSerializer.Serialize(login);

			_cache.Set(login.Refresh, bLogin, new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(REFRESH_EXPIRES_IN) });

			return login;
		}


		public async Task<UserLogin> Refresh(string refresh)
		{
			byte[] bLogin;

			try
			{
				await _semaphore.WaitAsync();

				bLogin = _cache.Get(refresh);

				_cache.Remove(refresh);
			}
			catch
			{
				return UserLogin.WithStatus(LoginStatus.InternalError);
			}
			finally
			{
				_semaphore.Release();
			}

			if (bLogin == null)
				return UserLogin.WithStatus(LoginStatus.Expiration);

			DateTime now = DateTime.UtcNow;
			UserLogin login = MessagePackSerializer.Deserialize<UserLogin>(bLogin);

			if (now.Subtract(login.LoginTime).TotalSeconds < JWT_EXPIRES_IN - 10.0)
			{
				login.Status = LoginStatus.Expiration;
				return login;
			}

			bool demoMode = _configuration.GetValue<bool>("Auth:DemoMode");

			var user = await _dbContext.Users.FindAsync(login.UserId);

			if (user == null || !user.IsEnabled || (user.IsDemo && !demoMode))
			{
				login.Status = LoginStatus.Forbidden;
				return login;
			}

			login.LoginTime = now;
			login.PreviousRefreshToken = refresh;
			login.Jwt = CreateJwt(user);
			login.Refresh = RandomString.Create(32);

			bLogin = MessagePackSerializer.Serialize(login);

			_cache.Set(login.Refresh, bLogin, new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(REFRESH_EXPIRES_IN) });

			return login;
		}

		public void SignOut(string refresh)
		{
			if (!string.IsNullOrEmpty(refresh))
				_cache.Remove(refresh);
		}
	}
}