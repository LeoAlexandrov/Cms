using System;
using System.ComponentModel.DataAnnotations.Schema;


namespace AleProjects.Cms.Domain.Entities
{

	public class DocumentPathNode
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public int DocumentRef { get; set; }

		public int Parent { get; set; }
		
		public int Position { get; set; }

		public Document Document { get; set; }
	}

}
