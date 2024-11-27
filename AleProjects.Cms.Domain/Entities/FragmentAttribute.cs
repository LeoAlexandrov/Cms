using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace AleProjects.Cms.Domain.Entities
{

	public class FragmentAttribute : IComparable<FragmentAttribute>
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public int FragmentRef { get; set; }

		
		[Required(AllowEmptyStrings = false), MaxLength(128)]
		public string AttributeKey { get; set; }

		public string Value { get; set; }

		public bool Enabled { get; set; }

		public Fragment Fragment { get; set; }

		// IComparable<FragmentAttribute> implementation

		public int CompareTo(FragmentAttribute other)
		{
			if (ReferenceEquals(this, other))
				return 0;

			if (other is null)
				return -1;

			return this.AttributeKey.CompareTo(other.AttributeKey);
		}

	}
}
