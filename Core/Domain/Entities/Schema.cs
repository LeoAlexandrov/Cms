using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;



namespace AleProjects.Cms.Domain.Entities
{
	public class Schema
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required(AllowEmptyStrings = false), MaxLength(256)]
		public string Namespace { get; set; }

		public string Description { get; set; }
		public string Data { get; set; }
		public DateTimeOffset ModifiedAt { get; set; }

	}
}
