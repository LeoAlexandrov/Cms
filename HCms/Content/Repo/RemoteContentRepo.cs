using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MessagePack;

using HCms.Content.ViewModels;


namespace HCms.Content.Repo
{
	/// <summary>
	/// Options for <see cref="RemoteContentRepo"/>.
	/// </summary>
	public class RemoteContentRepoOptions
	{
		public string CmsApiHost { get; set; }
		public string ApiKey { get; set; }
		public string PathMapperName { get; set; }
	}



	/// <summary>
	/// CMS content repository querying content via Rest or Grpc.
	/// </summary>
	public class RemoteContentRepo(IHttpClientFactory httpClientFactory, IOptions<RemoteContentRepoOptions> options, ILogger<RemoteContentRepo> logger) : IContentRepo
	{
		const string MSGPACK_MEDIA_TYPE = "application/x-msgpack";

		readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
		readonly string _cmsApiHost = options.Value.CmsApiHost?.TrimEnd('/');
		readonly string _apiKey = options.Value.ApiKey;
		readonly string _pathMapperName = options.Value.PathMapperName;
		readonly ILogger<RemoteContentRepo> _logger = logger;
		readonly Dictionary<string, string> _roles = [];


		[MessagePackObject(AllowPrivate = true)]
		internal struct UserRoleResponse
		{
			[MessagePack.Key("role")]
			public string Role { get; set; }
		}

		public void Reset()
		{
			_roles.Clear();
		}

		async static Task<T> RestRequest<T>(HttpClient client, string url, string apiKey, string acceptMediaType)
		{
			using HttpRequestMessage request = new()
			{
				Method = HttpMethod.Get,
				RequestUri = new Uri(url)
			};

			request.Headers.Add("APIKey", apiKey);

			if (!string.IsNullOrEmpty(acceptMediaType))
				request.Headers.Accept.Add(new(acceptMediaType));

			using HttpResponseMessage response = await client.SendAsync(request);

			response.EnsureSuccessStatusCode();

			string contentType = response.Content.Headers.ContentType?.MediaType;
			T result;

			if (contentType == MSGPACK_MEDIA_TYPE)
			{
				var stream = await response.Content.ReadAsStreamAsync();
				result = MessagePackSerializer.Deserialize<T>(stream);
			}
			else
			{
				result = await response.Content.ReadFromJsonAsync<T>();
			}

			return result;
		}

		public async Task<Document> GetDocument(string root, string path, int childrenFromPos, int takeChildren, bool siblings, int[] allowedStatus, bool exactPathMatch)
		{
			string ast = allowedStatus != null ? string.Join("&", allowedStatus.Select(s => $"ast={s}")) : "ast=1";
			string url = $"{_cmsApiHost}/api/v1/content/doc/{root}?path={path}&pm={_pathMapperName}&cfp={childrenFromPos}&tc={takeChildren}&sib={siblings}&{ast}";
			Document result;

			try
			{
				result = await RestRequest<Document>(_httpClientFactory.CreateClient(), url, _apiKey, MSGPACK_MEDIA_TYPE);
			}
			catch (HttpRequestException ex)
			{
				if (ex.StatusCode != System.Net.HttpStatusCode.NotFound)
				{
					_logger.LogError(ex, "Status code returned by H-Cms API is not 200 OK or 404 NotFound: {Url}", url);
					throw;
				}
				else
					result = null;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting document from the H-Cms API: {Url}", url);
				throw;
			}

			return result;
		}

		public async Task<Document> GetDocument(int id, int childrenFromPos, int takeChildren, bool siblings, int[] allowedStatus)
		{
			string ast = allowedStatus != null ? string.Join("&", allowedStatus.Select(s => $"ast={s}")) : "ast=1";
			string url = $"{_cmsApiHost}/api/v1/content/doc/{id}?pm={_pathMapperName}&cfp={childrenFromPos}&tc={takeChildren}&sib={siblings}&{ast}";
			Document result;

			try
			{
				result = await RestRequest<Document>(_httpClientFactory.CreateClient(), url, _apiKey, MSGPACK_MEDIA_TYPE);
			}
			catch (HttpRequestException ex)
			{
				if (ex.StatusCode != System.Net.HttpStatusCode.NotFound)
				{
					_logger.LogError(ex, "Status code returned by H-Cms API is not 200 OK or 404 NotFound: {Url}", url);
					throw;
				}
				else
					result = null;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting document from the H-Cms API: {Url}", url);
				throw;
			}

			return result;
		}

		public async ValueTask<string> UserRole(string login)
		{
			if (_roles.TryGetValue(login, out string role))
				return role;

			string url = $"{_cmsApiHost}/api/v1/content/role/{login}";
			UserRoleResponse result;

			try
			{
				result = await RestRequest<UserRoleResponse>(_httpClientFactory.CreateClient(), url, _apiKey, MSGPACK_MEDIA_TYPE);
				_roles[login] = result.Role;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting user role from remote CMS API: {Url}", url);
				throw;
			}

			return result.Role;
		}

	}

}