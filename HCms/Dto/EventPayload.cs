using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;


namespace HCms.Dto
{

	public class EventPayloadContentEntry
	{
		public int Id { get; set; }
		public string Root { get; set; }
		public string Path { get; set; }
	}



	public class EventPayload
	{
		[Required]
		public string Event { get; set; }

		public EventPayloadContentEntry[] AffectedContent { get; set; }
	}

}