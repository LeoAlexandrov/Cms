using System;


namespace HCms.Infrastructure.Notification
{

	public struct EventPayloadEntry
	{
		public int Id { get; set; }
		public string Root { get; set; }
		public string Path { get; set; }
	}



	public struct EventPayload
	{
		public string Event { get; set; }
		public EventPayloadEntry[] AffectedContent { get; set; }
	}



	public class NotificationEvent
	{

		public object[] Destinations { get; set; }
		public EventPayload Payload { get; set; }
	}
}