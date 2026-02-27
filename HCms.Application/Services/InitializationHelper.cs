using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using AleProjects.Base64;
using HCms.Application.Dto;
using HCms.Infrastructure.Notification;


namespace HCms.Application.Services
{

	internal struct InitialAttribute
	{
		public string Key { get; set; }
		public string Value { get; set; }
		public bool Private { get; set; }
	}



	internal struct InitialFragment
	{
		public string Name { get; set; }
		public string RawXml { get; set; }
		public InitialFragment[] Children { get; set; }
	}



	internal struct InitialDocument
	{
		public string Slug { get; set; }
		public string Title { get; set; }
		public string Language { get; set; }
		public InitialFragment[] Fragments { get; set; }
		public InitialDocument[] Children { get; set; }
		public InitialAttribute[] Attributes { get; set; }
	}



	public static class InitializationHelper
	{
#if DEBUG
		const string DATA_PATH = "InitialData/Docs";
		const string MEDIA_PATH = "InitialData/Media";
#else
		const string DATA_PATH = "InitialData/Docs";
		const string MEDIA_PATH = "InitialData/Media";
#endif

		public enum InitResult
		{
			Success,
			UsersExist,
			OtherProblem
		}


		static async Task CreateFragment(int docId, int ParentId, InitialFragment[] fragments, ContentManagementService cms, ClaimsPrincipal user)
		{
			foreach (var fr in fragments)
			{
				string xml;

				try
				{
					xml = File.ReadAllText($"{DATA_PATH}/{fr.RawXml}");
				}
				catch
				{
					continue;
				}

				var res = await cms.CreateFragment(new DtoCreateFragment() { Document = docId, Parent = ParentId, Name = fr.Name, Status = 1, RawXml = xml }, user);

				if (!res.Ok)
					continue;

				int linkId = res.Value.Link.Id;

				if (fr.Children != null)
				{
					await CreateFragment(docId, linkId, fr.Children, cms, user);
				}
			}
		}

		static async Task CreateDocument(int parentId, InitialDocument[] documents, ContentManagementService cms, ClaimsPrincipal user)
		{
			foreach (var doc in documents)
			{
				var res = await cms.CreateDocument(new DtoCreateDocument() { Parent = parentId, Slug = doc.Slug, Title = doc.Title, Language = doc.Language, Status = 1 }, user);

				if (!res.Ok)
					continue;

				int docId = res.Value.Properties.Id;

				if (doc.Fragments != null)
				{
					await CreateFragment(docId, 0, doc.Fragments, cms, user);
				}

				if (doc.Attributes != null)
				{
					foreach (var attr in doc.Attributes)
					{
						await cms.CreateAttribute(new DtoCreateDocumentAttribute() { DocumentRef = docId, AttributeKey = attr.Key, Value = attr.Value, Private = attr.Private, Enabled = true }, user);
					}
				}

				if (doc.Children != null)
				{
					await CreateDocument(docId, doc.Children, cms, user);
				}
			}
		}

		static async Task CreateMediaFile(string fileName, string destination, MediaManagementService mms, ClaimsPrincipal user)
		{
			using Stream stream = File.OpenRead(fileName);

			await mms.Save(stream, Path.GetFileName(fileName), destination, user);
		}

		static Task<Result<DtoEventDestinationLiteResult>> CreateEventDestination(string type, string name, string tPath, string tPathAux, EventDestinationManagementService eds, ClaimsPrincipal user)
		{
			object data = type switch
			{
				"webhook" => new WebhookDestination() { Endpoint = "https://localhost", Secret = "!#Secret_Code" },
				"redis" => new RedisDestination() { Endpoint = "localhost:6379", Channel = "hcms-channel" },
				"rabbitmq" => new RabbitMQDestination() { HostName = "localhost", Exchange = "hcms-exchange", ExchangeType = "fanout", RoutingKey = string.Empty },
				_ => null
			};

			return eds.CreateDestination(type, name, tPath, tPathAux, data, user);
		}

		static async Task CreateDemoData(IServiceScopeFactory serviceScopeFactory, ClaimsPrincipal user, ILogger logger)
		{
			InitialDocument[] documents;
			string[] files;

			try
			{
				string json = File.ReadAllText($"{DATA_PATH}/documents.json");
				documents = System.Text.Json.JsonSerializer.Deserialize<InitialDocument[]>(json);
			}
			catch (Exception ex)
			{
				logger?.LogError(ex, "Failed to load demo initialization data from JSON file");
				documents = [];
			}

			using var scope = serviceScopeFactory.CreateScope();
			var cms = scope.ServiceProvider.GetRequiredService<ContentManagementService>();

			await CreateDocument(0, documents, cms, user);

			var eds = scope.ServiceProvider.GetRequiredService<EventDestinationManagementService>();
			
			string tpath = documents[0].Slug + "/";
			string tpathAux = documents.Length > 1 ? documents[1].Slug + "/" : null;

			await CreateEventDestination("rabbitmq", "RabbitMQ", tpath, tpathAux, eds, user);
			await CreateEventDestination("redis", "Redis pub/sub", tpath, tpathAux, eds, user);
			await CreateEventDestination("webhook", "Webhook", tpath, tpathAux, eds, user);

			try
			{
				files = Directory.GetFiles(MEDIA_PATH, "*.*", SearchOption.TopDirectoryOnly);
			}
			catch (Exception ex)
			{
				logger?.LogError(ex, "Failed to list demo media files");
				files = [];
			}

			var mms = scope.ServiceProvider.GetRequiredService<MediaManagementService>();

			string destination = Base64Url.Encode(mms.GetDefaultPlace() ?? string.Empty);

			if (!string.IsNullOrEmpty(destination))
			{
				foreach (var f in files)
				{
					await CreateMediaFile(f, destination, mms, user);
				}

				if (Base64Url.TryDecode(destination, out tpath))
				{
					tpath += "/";

					await CreateEventDestination("rabbitmq", "RabbitMQ media", tpath, null, eds, user);
					await CreateEventDestination("redis", "Redis pub/sub media", tpath, null, eds, user);
					await CreateEventDestination("webhook", "Webhook media", tpath, null, eds, user);
				}

			}
		}

		public static async Task<InitResult> Initialize(IServiceScopeFactory serviceScopeFactory, string userLogin, bool addDemoData, ILoggerFactory loggerFactory)
		{
			using var scope = serviceScopeFactory.CreateScope();

			var ums = scope.ServiceProvider.GetRequiredService<UserManagementService>();

			if (!ums.NoUsers())
				return InitResult.UsersExist;

			var userResult = await ums.CreateUser(new() { Login = userLogin, Role = "Developer" }, null);

			if (!userResult.Ok)
				return InitResult.OtherProblem;


			if (addDemoData)
			{
				Claim[] claims = [
					new(ClaimTypes.NameIdentifier, userResult.Value.Login),
					new(ClaimTypes.Name, "cms"),
					new(ClaimTypes.Role, userResult.Value.Role)
				];

				ClaimsIdentity identity = new(claims, "AuthenticationTypes.Federation");

				var logger = loggerFactory?.CreateLogger("InitializationHelper");

				_ = CreateDemoData(serviceScopeFactory, new(identity), logger)
					.ContinueWith(t => logger?.LogError(t.Exception, "Creation of demo data failed"), TaskContinuationOptions.OnlyOnFaulted);
			}

			return InitResult.Success;
		}
	}

}