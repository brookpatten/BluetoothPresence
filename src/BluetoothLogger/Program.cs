using System;

using Ninject;

using BluetoothPresence.Configuration;

namespace BluetoothPresence
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			bool run = true;



			using (var kernel = BuildKernel())
			{
				//catch ctrl-c so that we can do a proper dispose & cleanup
				Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e) {
					e.Cancel = true;
					run = false;
					//shut down our bt connection
					kernel.Get<Mono.BlueZ.DBus.DBusConnection>().Dispose();
					Environment.Exit(0);
				};

				var service = kernel.Get<IBluetoothPresenceService>();
				while (run)
				{
					service.DiscoverAndLog();
				}
			}
		}

		private static IKernel BuildKernel()
		{
			var kernel = new StandardKernel();
			kernel.Load<BluetoothPresenceModule>();
			return kernel;
		}
	}
}
