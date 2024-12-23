using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;



namespace AleProjects.Cms.Domain.Entities
{
	public class Webhook
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required(AllowEmptyStrings = false), MaxLength(260)]
		public string Endpoint { get; set; }

		[Required(AllowEmptyStrings = false), MaxLength(32)]
		public string Secret { get; set; }

		public int RootDocument { get; set; }
		public bool Enabled { get; set; }
	}
}
