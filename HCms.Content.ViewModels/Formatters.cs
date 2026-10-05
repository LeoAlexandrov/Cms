using System;
using System.Globalization;

using MessagePack;
using MessagePack.Formatters;


namespace HCms.Content.ViewModels
{
	public sealed class IsoDateTimeOffsetFormatter : IMessagePackFormatter<DateTimeOffset>
	{
		public void Serialize(ref MessagePackWriter writer, DateTimeOffset value, MessagePackSerializerOptions options)
		{
			writer.Write(value.ToString("O", CultureInfo.InvariantCulture.DateTimeFormat));
		}

		public DateTimeOffset Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
		{
			string text = reader.ReadString();

			return string.IsNullOrEmpty(text)
				? throw new MessagePackSerializationException("Expected an ISO 8601 date/time string, but received nil.")
				: DateTimeOffset.Parse(text, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None);
		}
	}
}