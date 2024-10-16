using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using AleProjects.Cms.Domain.Entities;
using AleProjects.Cms.Domain.ValueObjects;


namespace AleProjects.Cms.Sdk.ViewModels
{
	public class Fragment : ITreeNode<int>
	{
		public int Id { get; set; }
		public int Container { get; set; }
		public string Name { get; set; }
		public string Icon { get; set; }
		public bool Shared { get; set; }
		public string XmlSchema { get; set; }
		public dynamic Props { get; set; }
		public Fragment[] Children { get; set; }

		// ITreeNode<int> implementation

		public int Parent => Container;
		public string Title => Name;
		public string Caption => null;
		public string Data { get; set; }

		public static Fragment Create(FragmentLink link, IList<XSElement> xse)
		{
			return new Fragment()
			{
				Id = link.FragmentRef,
				Container = link.Parent,
				Name = link.Title,
				Icon = link.Icon,
				Shared = link.Fragment.Shared,
				XmlSchema = link.Fragment.XmlSchema,
				Props = DynamicXml.Parse(link.Fragment.Data, xse),
				Data = link.Data
			};
		}
	}
}