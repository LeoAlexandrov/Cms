using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Xml.Serialization;


namespace AleProjects.Cms.Domain.Entities
{

	public class DocumentPathNode
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public int DocumentRef { get; set; }

		public int Parent { get; set; }
		
		[MessagePack.Key("position")]
		public int Position { get; set; }

		[MessagePack.IgnoreMember]
		[JsonIgnore]
		[XmlIgnore]
		public Document Document { get; set; }
	}

}
