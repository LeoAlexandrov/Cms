using System;
using System.ComponentModel.DataAnnotations.Schema;


namespace AleProjects.Cms.Domain.Entities
{
	public class Reference
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public int DocumentRef { get; set; }

		public int ReferenceTo { get; set; }

		public string MediaLink { get; set; }

		public Document Document { get; set; }
	}
}
