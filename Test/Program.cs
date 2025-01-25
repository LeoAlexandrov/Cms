using System;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;


using AleProjects.Base64;
using AleProjects.Cms;
using AleProjects.Cms.Application.Dto;
using AleProjects.Cms.Application.Services;
using AleProjects.Cms.Domain.Entities;
using AleProjects.Cms.Domain.ValueObjects;
using AleProjects.Cms.Infrastructure.Data;
using AleProjects.Cms.Infrastructure.Media;

using Sdk = AleProjects.Cms.Sdk;

using RazorEngineCore;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using MessagePack.Resolvers;
using MessagePack;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Test
{
	public class FragmentModel
	{
		public dynamic F { get; set; }
		public DtoFragmentResult Properties { get; set; }

	}

	public class DynamicXml : DynamicObject
	{
		private readonly string _ns;
		private readonly XSElement _xse;
		private readonly XElement _root;
		private readonly IFormatProvider _fmt;

		private DynamicXml(XElement root, IList<XSElement> xs)
		{
			_root = root;
			_fmt = new NumberFormatInfo();

			XAttribute attr;

			if (root != null && (attr = _root.Attribute("xmlns")) != null)
			{
				_ns = attr.Value;

				string name = _root.Name.LocalName;
				_xse = xs.FirstOrDefault(e => e.Name == name && e.Namespace == _ns);
			}
		}

		private DynamicXml(XElement root, string ns, IFormatProvider fmt, XSElement xse)
		{
			_root = root;
			_ns = ns;
			_fmt = fmt;
			_xse = xse;
		}

		public static DynamicXml Parse(string xmlString, IList<XSElement> xs)
		{
			return new DynamicXml(XDocument.Parse(xmlString).Root, xs);
		}

		private object Convert(string val, int type)
		{
			object result;

			switch (type)
			{
				case 1:
					if (int.TryParse(val, out int iRes))
						result = iRes;
					else
						result = val;
					break;

				case 2:
					if (double.TryParse(val, _fmt, out double xRes))
						result = xRes;
					else
						result = val;
					break;

				case 3:
					result = string.Compare(val, "true", true) == 0;
					break;

				default:
					result = val;
					break;
			}

			return result;
		}

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			string name = string.IsNullOrEmpty(_ns) ? binder.Name : string.Format("{{{0}}}{1}", _ns, binder.Name);

			var attr = _root.Attribute(name);

			if (attr != null)
			{
				result = attr.Value;
				return true;
			}

			var nodes = _root.Elements(name);
			var n = nodes.Count();

			if (n == 0)
			{
				result = null;
				return false;
			}

			XSElement newXse = null;
			int mtype = -1;
			bool isArray = false;

			if (_xse != null)
			{
				for (int i = 0; i < _xse.Elements.Count; i++)
					if (_xse.Elements[i].Name == binder.Name)
					{
						newXse = _xse.Elements[i];
						isArray = newXse.MaxOccurs > 1;

						if (newXse.IsSimple)
							mtype = newXse.XmlType switch
							{
								"int" or "integer" or "short" or "byte" => 1,
								"double" or "decimal" or "float" => 2,
								"boolean" or "bool" => 3,
								_ => 0,
							};

						break;
					}
			}

			if (n > 1)
			{
				result = nodes.Select(n => n.HasElements ? (object)new DynamicXml(n, _ns, _fmt, newXse) : Convert(n.Value, mtype)).ToArray();
				return true;
			}

			var node = nodes.FirstOrDefault();

			if (node.HasElements || node.HasAttributes)
			{
				result = new DynamicXml(node, _ns, _fmt, newXse);
				return true;
			}

			object res = Convert(node.Value, mtype);

			result = isArray ? new object[] { res } : res;

			return true;
		}
	}

	internal class Program
	{
		//https://github.com/moodmosaic/Fare


		const string codexml1 = @"<code-fragment
	xmlns=""http://aleprojects.com/test1.xsd""
	xmlns:t=""http://aleprojects.com/test1.xsd"">
	<language>Json</language>
	<code>dfsdfsd</code>
	<code>aaaaasss</code>
	<code>bbbbdfgdf</code>
</code-fragment>";

		const string codexml2 = @"<code-fragment
	xmlns=""http://aleprojects.com/test1.xsd""
	xmlns:t=""http://aleprojects.com/test1.xsd"">
	<language>Json</language>
	<code>dfsdfsd</code>
</code-fragment>";

		const string podxml = @"<pod
	xmlns=""http://aleprojects.com/test1.xsd""
	xmlns:t=""http://aleprojects.com/test1.xsd"">
	<appearance>
		<background>light</background>
		<layout>left</layout>
		<animation>true</animation>
	</appearance>
	<content>
		<title>DefaultTitle</title>
		<subtitle>Default subtitle</subtitle>
		<text><![CDATA[<p>sdadsasd</p><p>asdasdasd</p>]]></text>
		<text><![CDATA[<p>yyuiyuiyu</p><p>gdfgdfgdfg</p>]]></text>
	</content>
	<picture>
		<uri>25</uri>
	</picture>
	<button>
		<link>
			<uri></uri>
			<title>aaa</title>
			<altText>aaa1</altText>
			<icon>web</icon>
			<target>default</target>
		</link>
		<color>accent</color>
	</button>
</pod>";

		static Task<bool> AuthorizeAllOr(string[] policies)
		{
			Task<bool> taskChain = Task.FromResult(false);

			foreach (var policy in policies)
			{
				taskChain = taskChain.ContinueWith(async previousTask =>
				{
					Console.WriteLine("|| " + policy);

					if (previousTask.Result)
					{
						return true;
					}

					await Task.Delay(1000);
					return policy == "ok";

				}).Unwrap();
			}

			return taskChain;
		}

		static Task<bool> AuthorizeAllAnd(string[] policies)
		{
			Task<bool> taskChain = Task.FromResult(true);

			foreach (var policy in policies)
			{
				taskChain = taskChain.ContinueWith(async previousTask =>
				{
					Console.WriteLine("&& " + policy);

					if (!previousTask.Result)
					{
						return false;
					}

					await Task.Delay(1000);
					return policy == "ok";

				}).Unwrap();
			}

			return taskChain;
		}

		static async Task Main(string[] args)
		{
			string[] policies = ["asdasd", "asdasd", "qweqweqw", "sasdasdasd", "ok"];

			var res = await AuthorizeAllAnd(policies);
			Console.WriteLine(res);
			res = await AuthorizeAllOr(policies);
			Console.WriteLine(res);
			return;



			/*
			for (int i = 3; i < 50; i++)
			{
				for (int j = 0; j < 10; j++)
				{
					var s = RandomString.Create(i);
					var bytes = Encoding.UTF8.GetBytes(s);
					var b = Convert.ToBase64String(bytes);
					var b64 = Base64Url.Encode(s);
					Base64Url.TryDecode(b64, out string s64);

					Console.WriteLine(s + " | " + s64 + " | " + bytes.Length.ToString() + " | " + b64); // + " | " + b);
				}
			}

			return;
			*/


			/*
			var  mobj = new {
				Title = "asdadsas",
				Text = "dfsdfsdd",
				Items = new int[] { 1, 2, 34, 45 },
				Link = new
				{
					Url = "https://h-cms.net",
					Title = "h-cms"
				}
			};

			var blob = MessagePackSerializer.Serialize(mobj, ContractlessStandardResolver.Options);

			// Dynamic ("untyped")
			var dynamicModel = MessagePackSerializer.Deserialize<dynamic>(blob, ContractlessStandardResolver.Options);

			// You can access the data using array/dictionary indexers, as shown above
			Console.WriteLine(dynamicModel["Title"]); 
			Console.WriteLine(dynamicModel["Items"][2]);

			return;
			*/

			/*
			XSElement.XSAnnotation? ann = new XSElement.XSAnnotation()
			{
				Documentation = [ ]
			};

			//ann = null;

			if (ann == null)
			{
				Console.WriteLine(ann.Value.ToString());
			}
			*/

			IConfiguration config = new ConfigurationBuilder()
				.AddJsonFile(Path.GetFullPath("../../../../../Cms-configs/settings.json"), optional: true, reloadOnChange: false)
			.Build();


			string connString = config.GetConnectionString("CmsDbConnection");

			// check if something changed in Documents table 
			//var cs = dc.Database.SqlQueryRaw<int>($"select CHECKSUM_AGG(BINARY_CHECKSUM(*)) as [Value] From Documents with (Nolock)");
			//int agg = cs.FirstOrDefault();

			var rt = new Sdk.Routing.DefaultPathTransformer(config);

			var cp = new Sdk.ContentRepo.ContentRepo(rt, config);
			//var doc = cp.GetDocument("home1", null, -1, true, null).Result; //"/new4/new2"
			var doc = cp.GetDocument(12, 0, true).Result;

			var (root, path) = cp.IdToPath(12).Result;

			return;




			var contextOptions = new DbContextOptionsBuilder<CmsDbContext>().UseSqlServer(connString).Options;
			var dc = new CmsDbContext(contextOptions);


			var fs = new FragmentSchemaRepo(dc);
			var xsePod = fs.Fragments.FirstOrDefault(f => f.Name == "pod");

			dynamic pod = DynamicXml.Parse(podxml, fs.Fragments);
			var ap = pod.appearance;
			var bk = ap.background;
			var an = ap.animation;
			var st = pod.content.text;
			Console.WriteLine(bk);

			dynamic code = DynamicXml.Parse(codexml1, fs.Fragments);
			var cd = code.code;

			foreach (string c in cd) 
				Console.WriteLine(c);

			return;

			/*
			var fi = new FileInfo(@"M:\cms-media\Fr");

			using (var stream = File.OpenRead(@"M:\cms-media\IMG20240126112035.jpg"))
			using (var output = File.OpenWrite(@"M:\cms-media\IMG20240126112035_64x64.webp"))
			{
				//var pngEncoder = new PngEncoder() { ColorType = PngColorType.RgbWithAlpha };
				//var webpEncoder = new WebpEncoder() {  ColorType = PngColorType.RgbWithAlpha };

				var image = Image.Load<Rgba32>(stream);
				image.Mutate(x =>
					x.Resize(new ResizeOptions() { Mode = ResizeMode.Pad, Size = new(64, 64), PadColor = Color.Transparent })
					//.BackgroundColor(Color.Transparent)
					);

				//image.SaveAsPng(output, pngEncoder);
				image.SaveAsWebp(output);
			}

			return;
			*/


			var obj = pod;

			string templateText = @"

@foreach (var st in Model.F.content.text)
{
	@st
}

@for (int i = 0; i < 5; i++)
{
<h1>@Model.F.appearance.background</h1>

@if (@Model.F.appearance.animation)
{
<p>Animation</p>
}
}
";
			//

			try
			{
				RazorEngine razorEngine = new();

				//IRazorEngineCompiledTemplate<RazorEngineTemplateBase<JsonDoc.JsonObject>> template = 
				//	razorEngine.Compile<RazorEngineTemplateBase<JsonDoc.JsonObject>>(templateText);

				IRazorEngineCompiledTemplate<RazorEngineTemplateBase<FragmentModel>> template = 
					razorEngine.Compile<RazorEngineTemplateBase<FragmentModel>>(templateText, builder =>
				{
					builder.AddAssemblyReferenceByName("System.Collections"); // by name
					//builder.AddAssemblyReference(typeof(System.IO.File)); // by type
					//builder.AddAssemblyReference(Assembly.Load("source")); // by reference
				});

				//IRazorEngineCompiledTemplate template = razorEngine.Compile("Hello @Model.Name");
				//string result = template.Run(doc);

				template.SaveToFile(@"C:\Temp\template.bin");

				var t = RazorEngineCompiledTemplate<RazorEngineTemplateBase<FragmentModel>>.LoadFromFile(@"C:\Temp\template.bin");


				string result = t.Run(instance =>
				{
					instance.Model = new FragmentModel() { F = pod, Properties = null };
				});

				Console.WriteLine(result);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}


		}

	}
}