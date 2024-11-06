using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using AleProjects.Cms.Domain.Entities;
using AleProjects.Cms.Domain.ValueObjects;


namespace AleProjects.Cms.Sdk.ViewModels
{
	public class Fragment : ITreeNode<int>
	{
		public int Id { get; set; }
		public int LinkId { get; set; }
		public int Container { get; set; }
		public string Name { get; set; }
		public string Icon { get; set; }
		public bool Shared { get; set; }
		public string XmlName { get; set; }
		public string XmlSchema { get; set; }
		public dynamic Props { get; set; }
		public Dictionary<string, Attribute> Attributes { get; set; }
		public Document Document { get; set; }
		public Fragment[] Children { get; set; }

		// ITreeNode<int> implementation

		public int Parent => Container;
		public string Title => Name;
		public string Caption => null;
		public string Data { get; set; }

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
				XmlName = link.Fragment.XmlName,
				XmlSchema = link.Fragment.XmlSchema,
				Props = DynamicXml.Parse(link.Fragment.Data, xse),
				Attributes = attrs.ToDictionary(a => a.AttributeKey, a => new Attribute() { Id = a.Id, Value = a.Value, Enabled = a.Enabled }),
				Document = doc,
				Data = link.Data
			};
		}
	}
}