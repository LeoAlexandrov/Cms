using System;
using System.Threading.Tasks;


namespace HCms.Infrastructure.Notification
{

	public class EventPayloadContentEntry
	{
		public int Id { get; set; }
		public string Root { get; set; }
		public string Path { get; set; }
	}



	public class EventPayload
	{
		public string Event { get; set; }
		public EventPayloadContentEntry[] AffectedContent { get; set; }
	}

}