using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

using MessagePack;
using MessagePack.Resolvers;


namespace AleProjects.Cms.Web.Infrastructure.MediaTypeFormatters
{

	public class MessagePackOutputFormatter : OutputFormatter
	{
		private const string MediaType = "application/x-msgpack";
		private readonly MessagePackSerializerOptions _options;

		public MessagePackOutputFormatter() : this(null)
		{
		}

		public MessagePackOutputFormatter(MessagePackSerializerOptions options)
		{
			SupportedMediaTypes.Add(new MediaTypeHeaderValue(MediaType));

			_options = options ??
				MessagePackSerializerOptions
					.Standard
					.WithResolver(StandardResolverAllowPrivate.Instance);
		}


		protected override bool CanWriteType(Type type)
		{
			ArgumentNullException.ThrowIfNull(type);

			return IsAllowedType(type);
		}

		private static bool IsAllowedType(Type t)
		{
			if (t != null && !t.IsAbstract && !t.IsInterface && !t.IsNotPublic)
				return true;

			if (typeof(IEnumerable<>).IsAssignableFrom(t))
				return true;

			return false;
		}

		public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
		{
			await context.HttpContext.Response.Body.WriteAsync(MessagePackSerializer.Serialize(context.Object, _options));
		}
	}

}