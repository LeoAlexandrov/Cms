using System;
using System.ComponentModel.DataAnnotations.Schema;

using AleProjects.Cms.Domain.ValueObjects;



namespace AleProjects.Cms.Domain.Entities
{
	public class FragmentLink : ITreeNode<int>, IComparable<FragmentLink>
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public int DocumentRef { get; set; }

		public int FragmentRef { get; set; }

		public int ContainerRef { get; set; }

		public int Position { get; set; }

		public bool Enabled { get; set; }

		public bool Anchor { get; set; }

		public Document Document { get; set; }

		public Fragment Fragment { get; set; }

		// IComparable<FragmentLink> implementation

		public int CompareTo(FragmentLink other)
		{
			if (ReferenceEquals(this, other))
				return 0;

			if (other is null)
				return -1;

			return this.Id.CompareTo(other.Id);
		}

		// ITreeNode<int> implementation

		[NotMapped]
		public int Parent => ContainerRef;

		[NotMapped]
		public string Title => Fragment?.Name;

		[NotMapped]
		public string Caption => Fragment?.XmlName;

		[NotMapped]
		public string Icon => Fragment?.Icon;

		[NotMapped]
		public string Data { get; set; }

	}
}
