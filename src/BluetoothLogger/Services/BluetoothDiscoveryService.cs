using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using DBus;
using Mono.BlueZ.DBus;
using org.freedesktop.DBus;

namespace BluetoothPresence
{
	public class BluetoothPresenceService:IBluetoothPresenceService
	{
		private DBusConnection _connection;
		private ObjectManager _objectManager;
		private string _adapter;
		private IBluetoothRepository _repository;

		public BluetoothPresenceService(DBusConnection connection,string adapter,IBluetoothRepository repository)
		{
			_connection = connection;
			_adapter = adapter;
			_repository = repository;
		}

		public void DiscoverAndLog()
		{
			//TODO: add an actual logger
			System.Console.WriteLine("Starting Discovery...");

			//get a copy of the object manager so we can browse the "tree" of bluetooth items
			var manager = _connection.System.GetObject<ObjectManager> (BlueZPath.Service, ObjectPath.Root);

			var adapter = _connection.System.GetObject<Adapter1>(BlueZPath.Service, BlueZPath.Adapter(_adapter));

			var timeout = adapter.DiscoverableTimeout;

			if (adapter.Discovering)
			{
				try
				{
					adapter.StopDiscovery();
				}
				catch 
				{
					//sometimes this throws if the discovery ended between when we checked and when we tell it to stop
					//yuck
				}
			}
			//start discovery
			adapter.StartDiscovery();

			//register these events so we can tell when things are added/removed (eg: discovery)
			manager.InterfacesAdded += InterfaceAdded;
			manager.InterfacesRemoved += InterfaceRemoved;

			//yuck, should do this async
			System.Threading.Thread.Sleep((int)timeout*1000);
		}

		private void InterfaceAdded(ObjectPath p,IDictionary<string,IDictionary<string,object>> i)
		{
			if (i.Keys.Contains(typeof(Device1).DBusInterfaceName()))
			{
				string address = GetAddressFromPath(p);
				System.Console.WriteLine (address + " Discovered");

				var device = _connection.System.GetObject<Device1>(BlueZPath.Service, p);

				string name = "?";
				int? rssi = null;
				try
				{
					name = device.Name;
				}
				catch
				{
					//this property invokes a dbus call so it can fail and it throws a generic exception
					//if the name isn't returned
				}

				try
				{
					rssi = device.RSSI;
				}
				catch
				{
					//same as above
				}

				var deviceModel = _repository.FindDeviceByAddress(address);
				if (deviceModel == null)
				{
					System.Console.WriteLine("\tDevice is new");
					deviceModel = _repository.CreateDevice(address, name);
				}
				else
				{
					System.Console.WriteLine("\tDevice is existing");
				}

				var appearance = _repository.FindMostRecentAppearance(address);
				if (appearance == null || appearance.EndedAt.HasValue)
				{
					System.Console.WriteLine("\tDevice is now present");
					_repository.CreateApperance(deviceModel.Id, DateTime.UtcNow, rssi);
				}
			}
		}

		private void InterfaceRemoved(ObjectPath path, string[] interfaces)
		{
			if (interfaces.Contains(typeof(Device1).DBusInterfaceName()))
			{
				string address = GetAddressFromPath(path);
				System.Console.WriteLine (address + " Lost");

				var deviceModel = _repository.FindDeviceByAddress(address);
				if (deviceModel == null)
				{
					//we lost a device we didn't know about.... what to do about that
				}

				var appearance = _repository.FindMostRecentAppearance(address);
				if (appearance != null && !appearance.EndedAt.HasValue)
				{
					System.Console.WriteLine("\tDevice is no longer present");
					appearance.EndedAt = DateTime.UtcNow;
					_repository.UpdateAppearance(appearance);
				}
			}
		}

		private string GetAddressFromPath(ObjectPath path)
		{
			//built with 
			//https://txt2re.com/index-csharp.php3?s=/org/bluez/hci0/dev_F6_58_7F_09_5D_E6&15&12&20&30&11&17&27&14&16&-54&-9&-55&-3&-56&-57&-7&-8&-71&-72&-73&-74&-75&-76
			//because I'm lazy

			string re1="(\\/)";	// Any Single Character 1
			string re2="(org)";	// Word 1
			string re3="(\\/)";	// Any Single Character 2
			string re4="(bluez)";	// Word 2
			string re5="(\\/)";	// Any Single Character 3
			string re6="(hci)";	// Word 3
			string re7="(\\d+)";	// Integer Number 1
			string re8="(\\/)";	// Any Single Character 4
			string re9="(dev)";	// Word 4
			string re10="(_)";	// Any Single Character 5
			string re11="((?:[a-fA-F0-9]{2}))";	// Alphanum 1
			string re12="(_)";	// Any Single Character 6
			string re13="((?:[a-fA-F0-9]{2}))";	// Alphanum 1
			string re14="(_)";	// Any Single Character 7
			string re15="((?:[a-fA-F0-9]{2}))";	// Alphanum 1
			string re16="(_)";	// Any Single Character 8
			string re17="((?:[a-fA-F0-9]{2}))";	// Alphanum 1
			string re18="(_)";	// Any Single Character 9
			string re19="((?:[a-fA-F0-9]{2}))";	// Alphanum 1
			string re20="(_)";	// Any Single Character 10
			string re21="((?:[a-fA-F0-9]{2}))";	// Alphanum 1

			Regex r = new Regex(re1+re2+re3+re4+re5+re6+re7+re8+re9+re10+re11+re12+re13+re14+re15+re16+re17+re18+re19+re20+re21,RegexOptions.IgnoreCase|RegexOptions.Singleline);
			Match m = r.Match(path.ToString());
			if (m.Success)
			{
				string address = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", 
				                               m.Groups[11].ToString(),
				                               m.Groups[13].ToString(),
				                               m.Groups[15].ToString(),
				                               m.Groups[17].ToString(),
				                               m.Groups[19].ToString(),
				                               m.Groups[21].ToString());
				return address;
			}
			else
			{
				throw new FormatException("Bad dbus path for device");
			}
		}

	}
}

