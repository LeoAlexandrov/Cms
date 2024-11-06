using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace AleProjects.Cms.Domain.Entities
{
	public class Fragment
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required(AllowEmptyStrings = false), MaxLength(256)]
		public string Name { get; set; }

		[MaxLength(64)]
		public string Icon { get; set; }

		public bool Shared { get; set; }

		[Required(AllowEmptyStrings = false), MaxLength(256)]
		public string XmlSchema { get; set; }

		[Required(AllowEmptyStrings = false), MaxLength(256)]
		public string XmlName { get; set; }

		[Required(AllowEmptyStrings = false)]
		public string Data { get; set; }

		public List<FragmentLink> DocumentLinks { get; set; }

		public List<FragmentAttribute> FragmentAttributes { get; set; }
	}
}
