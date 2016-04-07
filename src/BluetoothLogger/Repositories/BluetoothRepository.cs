using System;
using System.Data;
using System.Linq;

using Dapper;

namespace BluetoothPresence
{
	public class BluetoothRepository:IBluetoothRepository
	{
		private IDbConnection _connection;
		public BluetoothRepository(IDbConnection connection)
		{
			_connection = connection;
			VerifySchema();
		}

		public void CreateApperance(long deviceId, DateTime startedAt,int? rssi)
		{
			_connection.Execute("insert into Appearances(DeviceId,StartedAt,RSSI) values (@deviceId,@startedAt,@rssi)", new { deviceId, startedAt, rssi });
		}

		public Device CreateDevice(string address, string name)
		{
			_connection.Execute("insert into Devices(Address,Name) values (@address,@name)", new { address, name });
			var deviceId = (long)_connection.ExecuteScalar("select id from Devices where Address=@address", new { address });
			return new Device() { Id = deviceId, Address = address, Name = name };
		}

		public Device FindDeviceByAddress(string address)
		{
			var device = _connection.Query<Device>("select * from Devices where Address=@address", new { address })
				.SingleOrDefault();
			return device;
		}

		public Appearance FindMostRecentAppearance(string address)
		{
			var appearance = _connection.Query<Appearance>("select * from Appearances a inner join Devices d on d.Id=a.DeviceId where d.Address=@address order by a.StartedAt desc", new { address })
				.FirstOrDefault();
			return appearance;
		}

		public void UpdateAppearance(Appearance appearance)
		{
			_connection.Execute("update Appearances set EndedAt=@EndedAt where Id=@Id", new { appearance.EndedAt, appearance.Id });
		}

		//TODO: move this to a migration or somewhere smart
		private void VerifySchema()
		{
				if(!_connection.Query<string> ("SELECT name FROM sqlite_master WHERE type='table' and name='Devices';").Any())
				{
					//if the table doesn't exist, create it
					_connection.Execute("create table Devices(" +
					                "Id INTEGER PRIMARY KEY," +
				                    "Address Text,"+
					                "Name Text," +
					                "FriendlyName Text" +
					                ")");

					_connection.Execute ("create table Appearances(" +
					                 "Id INTEGER PRIMARY KEY," +
					                 "DeviceId INTEGER," +
					                 "StartedAt DATETIME,"+
				                     "EndedAt DATETIME,"+
				                     "RSSI INTEGER"+
					                 ")");


				}
		}
	}
}

