using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

using MessagePack;
using MessagePack.Resolvers;


namespace AleProjects.Cms.Web.Infrastructure.MediaTypeFormatters
{

	public class MessagePackInputFormatter : InputFormatter
	{
		private const string MediaType = "application/x-msgpack";
		private readonly MessagePackSerializerOptions _options;

		public MessagePackInputFormatter() : this(null)
		{
		}

		public MessagePackInputFormatter(MessagePackSerializerOptions options)
		{
			SupportedMediaTypes.Add(new MediaTypeHeaderValue(MediaType));

			_options = options ??
				MessagePackSerializerOptions
					.Standard
					.WithResolver(StandardResolverAllowPrivate.Instance);
		}


		protected override bool CanReadType(Type type)
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

		public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
		{
			try
			{
				var result = await MessagePackSerializer.DeserializeAsync(context.ModelType, context.HttpContext.Request.Body, _options);
				return await InputFormatterResult.SuccessAsync(result);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex);
				return await InputFormatterResult.FailureAsync();
			}
		}

	}

}