using System;
namespace BluetoothPresence
{
	public class Appearance
	{
		public long Id { get; set; }
		public long DeviceId { get; set; }
		public DateTime StartedAt { get; set; }
		public DateTime? EndedAt { get; set; }
		public int? RSSI { get; set; }
	}
}

