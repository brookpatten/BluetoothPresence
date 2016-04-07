using System;
namespace BluetoothPresence
{
	public interface IBluetoothRepository
	{
		Device FindDeviceByAddress(string address);
		Device CreateDevice(string address, string name);
		Appearance FindMostRecentAppearance(string address);
		void UpdateAppearance(Appearance appearance);
		void CreateApperance(long deviceId, DateTime startedAt,int? rssi);
	}
}

