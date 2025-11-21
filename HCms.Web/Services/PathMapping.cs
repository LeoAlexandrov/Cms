using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Configuration;

using HCms.Content.Services;


namespace HCms.Web.Services
{

	public class PathMapperConfig
	{
		public struct MediaConfig
		{
			public string PathRegex { get; set; }
			public string UrlReplace { get; set; }
		}

		public struct PageConfig
		{
			[Required]
			public string Root { get; set; }

			public string PathRegex { get; set; }
			public string UrlReplace { get; set; }
		}

		[Required]
		public string Name { get; set; }

		public MediaConfig[] Media { get; set; }
		public PageConfig[] Pages { get; set; }
	}



	public class PathMappingOptions
	{
		public string ExternalMapperLocation { get; set; }
		public PathMapperConfig[] PathMappers { get; set; }
	}



	public class PathMapper : IPathMapper
	{
		const string HTTPS = "https://";
		const string HTTP = "http://";

		delegate string MapDelegate(string root, string path, bool media);
		readonly MapDelegate _map;
		readonly MappingParams[] _mappingParams;


		struct MappingParams
		{
			public string Root { get; set; }
			public Regex Rx { get; set; }
			public string UrlReplace { get; set; }
		}

		public PathMapper(MethodInfo mapMethod)
		{
			_map = mapMethod == null ? MapDefault : (MapDelegate)Delegate.CreateDelegate(typeof(MapDelegate), mapMethod);
		}

		public PathMapper(PathMapperConfig config)
		{
			if (config == null)
			{
				_map = MapDefault;
				return;
			}

			int n = config.Media?.Length ?? 0;
			int m = config.Pages?.Length ?? 0;

			if (n + m == 0)
			{
				_map = MapDefault;
				return;
			}

			_mappingParams = new MappingParams[n + m];

			for (int i = 0; i < n; i++)
				_mappingParams[i] = new MappingParams()
				{
					Rx = new Regex(config.Media[i].PathRegex ?? "^(.*)"),
					UrlReplace = config.Media[i].UrlReplace ?? "$1"
				};

			for (int i = 0; i < m; i++)
				_mappingParams[i + n] = new MappingParams()
				{
					Root = config.Pages[i].Root,
					Rx = new Regex(config.Pages[i].PathRegex ?? "^(.*)"),
					UrlReplace = config.Pages[i].UrlReplace ?? "$1"
				};


			_map = MapConfigured;
		}

		static string MapDefault(string root, string path, bool media) => path;

		string MapConfigured(string root, string path, bool media = false)
		{
			if (string.IsNullOrWhiteSpace(path))
				throw new ArgumentException("Path is required for mapping.", nameof(path));

			if (path.StartsWith(HTTPS, StringComparison.OrdinalIgnoreCase) ||
				path.StartsWith(HTTP, StringComparison.OrdinalIgnoreCase))
				return path;

			if (string.IsNullOrWhiteSpace(root) && !media)
				throw new ArgumentNullException(nameof(root), "Root document slug is required for path mapping.");


			if (media)
			{
				foreach (var mParams in _mappingParams.Where(p => string.IsNullOrEmpty(p.Root)))
				{
					string result = mParams.Rx.Replace(path, mParams.UrlReplace);

					if (result != path)
					{
						path = result;
						break;
					}
				}
			}
			else
			{
				foreach (var mParams in _mappingParams.Where(p => string.Equals(p.Root, root, StringComparison.OrdinalIgnoreCase)))
				{
					string result = mParams.Rx.Replace(path, mParams.UrlReplace);

					if (result != path)
					{
						path = result;
						break;
					}
				}
			}

			return path;
		}

		public string Map(string root, string path, bool media) => _map(root, path, media);
	}



	public class PathMapperFactory : IPathMapperFactory
	{
		private readonly Assembly _assembly;
		private readonly Dictionary<string, IPathMapper> _cache;

		private PathMapperFactory(Assembly assembly, PathMapperConfig[] configs)
		{
			_assembly = assembly;
			_cache = [];

			if (configs != null)
				foreach (var cfg in configs)
					_cache[cfg.Name] = new PathMapper(cfg);
		}

		public static PathMapperFactory Load(IConfiguration config)
		{
			var section = config.GetSection("PathMappingOptions");
			var options = section?.Get<PathMappingOptions>();

			if (options == null)
				return new PathMapperFactory(null, null);

			string scriptPath = options.ExternalMapperLocation;
			Assembly assembly = null;

			if (File.Exists(scriptPath))
			{
				if (scriptPath.EndsWith(".dll"))
				{
					assembly = Assembly.LoadFrom(scriptPath);
				}
				else
				{
					var code = File.ReadAllText(scriptPath);
					var syntaxTree = CSharpSyntaxTree.ParseText(code);

					var references = AppDomain.CurrentDomain.GetAssemblies()
						.Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
						.Select(a => MetadataReference.CreateFromFile(a.Location));

					var compilation = CSharpCompilation.Create(
						"ExternalPathMapperLib",
						[syntaxTree],
						references,
						new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
					);

					using var ms = new MemoryStream();
					var result = compilation.Emit(ms);

					if (!result.Success)
					{
						string errors = string.Join("\n", result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
						throw new Exception($"Compilation of '{scriptPath}' failed:\n{errors}");
					}

					ms.Seek(0, SeekOrigin.Begin);

					assembly = Assembly.Load(ms.ToArray());
				}
			}

			return new PathMapperFactory(assembly, options.PathMappers);
		}

		public IPathMapper Get(string name)
		{
			if (string.IsNullOrEmpty(name))
				return new PathMapper(mapMethod: null);

			if (_cache.TryGetValue(name, out var mapper))
				return mapper;

			if (_assembly != null)
			{
				var type = _assembly.GetType("ExternalScriptedPathMapper");
				var method = type?.GetMethod(name, BindingFlags.Public | BindingFlags.Static);

				if (method != null)
				{
					mapper = new PathMapper(method);
					_cache[name] = mapper;
				}
			}

			return mapper;
		}

	}
}