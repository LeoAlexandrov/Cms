using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace AleProjects.Cms.Domain.Entities
{

	public class DocumentAttribute : IComparable<DocumentAttribute>
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public int DocumentRef { get; set; }

		
		[Required(AllowEmptyStrings = false), MaxLength(128)]
		public string AttributeKey { get; set; }

		public string Value { get; set; }

		public bool Enabled { get; set; }

		public Document Document { get; set; }

		// IComparable<DocumentAttribute> implementation

		public int CompareTo(DocumentAttribute other)
		{
			if (ReferenceEquals(this, other))
				return 0;

			if (other is null)
				return -1;

			return this.AttributeKey.CompareTo(other.AttributeKey);
		}

	}
}
