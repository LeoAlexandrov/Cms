using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using MessagePack;


namespace HCms.Content.ViewModels
{
	/// <summary>
	/// Represents a fragment view model. For use as partial view model.
	/// </summary>
	[MessagePackObject]
	public class Fragment
	{
		[MessagePack.Key("id")]
		public int Id { get; set; }


		[MessagePack.Key("linkId")]
		public int LinkId { get; set; }

		[MessagePack.IgnoreMember]
		[JsonIgnore]
		public string DomId => GetDomId();

		[MessagePack.Key("container")]
		public int Container { get; set; }

		[MessagePack.Key("name")]
		public string Name { get; set; }

		[MessagePack.Key("icon")]
		public string Icon { get; set; }

		[MessagePack.Key("shared")]
		public bool Shared { get; set; }

		[MessagePack.Key("status")]
		public int Status { get; set; }

		[MessagePack.Key("anchor")]
		public bool Anchor { get; set; }

		[MessagePack.Key("xmlName")]
		public string XmlName { get; set; }

		[MessagePack.Key("xmlSchema")]
		public string XmlSchema { get; set; }

		[MessagePack.IgnoreMember]
		[JsonIgnore]
		public dynamic Props { get; private set; }

		[MessagePack.Key("props")]
		[JsonPropertyName("props")]
		public IReadOnlyDictionary<string, object> Props2 
		{
			get => Props?.ToDictionary();
			set => Props = new DynamicProperties(value);
		}

		/*
		[MessagePack.IgnoreMember]
		[JsonIgnore]
		public ByteString Props3 
		{
			get
			{
				var b = MessagePack.MessagePackSerializer.Serialize(Props2);
				var p = MessagePack.MessagePackSerializer.Deserialize<Dictionary<string, object>>(b);
				return ByteString.CopyFrom(b);
			}
			set => Props2 = MessagePack.MessagePackSerializer.Deserialize<Dictionary<string, object>>(value.Memory); 
		}
		*/

		[MessagePack.Key("attributes")]
		public Dictionary<string, string> Attributes { get; set; }

		[MessagePack.Key("children")]
		public Fragment[] Children { get; set; }

		[MessagePack.IgnoreMember]
		[JsonIgnore]
		public string BasicCssClass => Container == 0 ? $"{XmlName}-fragment" : $"{XmlName}-inner-fragment";


		public Fragment() 
		{
		}

		public Fragment(Fragment fragment, string xmlName, dynamic props, Dictionary<string, string> attrs = null)
		{
			if (fragment != null)
			{
				Id = 0;
				LinkId = fragment.LinkId;
				Container = fragment.Id;
				Name = fragment.Name;
				Icon = fragment.Icon;
				Shared = fragment.Shared;
				Status = fragment.Status;
				XmlName = xmlName;
				XmlSchema = fragment.XmlSchema;
				Props = props;
				Attributes = attrs ?? [];
			}
		}


		string GetDomId()
		{
			if (Id == 0 || string.IsNullOrEmpty(Name))
				return null;

			int n = Name.Length;
			Span<char> cId = stackalloc char[n];

			for (int i = 0; i < n; i++)
				if (Name[i] == '-' || Name[i] == '_' || char.IsLetterOrDigit(Name[i]))
					cId[i] = Name[i];
				else
					cId[i] = '-';

			return new string(cId);
		}
	}
}