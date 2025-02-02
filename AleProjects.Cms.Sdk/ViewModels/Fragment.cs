﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AleProjects.Cms.Domain.Entities;
using AleProjects.Cms.Domain.ValueObjects;


namespace AleProjects.Cms.Sdk.ViewModels
{
	public class Fragment : ITreeNode<int>
	{
		public int Id { get; set; }
		public int LinkId { get; set; }
		public string DomId { get => GetDomId(); }
		public int Container { get; set; }
		public string Name { get; set; }
		public string Icon { get; set; }
		public bool Shared { get; set; }
		public bool Anchor { get; set; }
		public string XmlName { get; set; }
		public string XmlSchema { get; set; }
		public dynamic Props { get; set; }
		public Dictionary<string, string> Attributes { get; set; }
		public Document Document { get; set; }
		public Fragment[] Children { get; set; }

		// ITreeNode<int> implementation

		public int Parent => Container;
		public string Title => Name;
		public string Caption => null;
		public string Data { get; set; }

		public bool Enabled => true;

		public Fragment() { }

		public Fragment(Fragment fragment, string xmlName, dynamic props)
		{
			if (fragment != null)
			{
				Id = 0;
				LinkId = fragment.LinkId;
				Container = fragment.Id;
				Name = fragment.Name;
				Icon = fragment.Icon;
				Shared = fragment.Shared;
				XmlName = xmlName;
				XmlSchema = fragment.XmlSchema;
				Props = props;
				Attributes = fragment.Attributes;
				Document = fragment.Document;
				Data = fragment.Data;
			}
		}

		public static Fragment Create(FragmentLink link, Document doc, IEnumerable<FragmentAttribute> attrs, IList<XSElement> xse)
		{
			return new Fragment()
			{
				Id = link.FragmentRef,
				LinkId = link.Id,
				Container = link.Parent,
				Name = link.Title,
				Icon = link.Icon,
				Shared = link.Fragment.Shared,
				Anchor = link.Anchor,
				XmlName = link.Fragment.XmlName,
				XmlSchema = link.Fragment.XmlSchema,
				Props = DynamicXml.Parse(link.Fragment.Data, xse),
				Attributes = attrs.ToDictionary(a => a.AttributeKey, a => a.Value),
				Document = doc,
				Data = link.Data
			};
		}

		private string GetDomId()
		{
			if (Id == 0)
				return null;

			int n = Name.Length;
			Span<char> cId = stackalloc char[n];

			for (int i = 0; i < n; i++)
				if (Name[i] == '-' || Name[i] == '-' || char.IsLetterOrDigit(Name[i]))
					cId[i] = Name[i];
				else
					cId[i] = '-';

			return new string(cId);
		}
	}
}