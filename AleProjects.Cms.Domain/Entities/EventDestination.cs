using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;



namespace AleProjects.Cms.Domain.Entities
{
	public class EventDestination
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required(AllowEmptyStrings = false), MaxLength(16)]
		public string Type { get; set; }

		[Required(AllowEmptyStrings = false), MaxLength(128)]
		public string Name { get; set; }

		public string TriggeringPath { get; set; }

		public string TriggeringPathAux { get; set; }

		public bool Enabled { get; set; }

		[Required(AllowEmptyStrings = false)]
		public string Data { get; set; }
	}
}
