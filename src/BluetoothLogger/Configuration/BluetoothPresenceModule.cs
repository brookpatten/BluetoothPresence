﻿using System;
using System.Data;
using System.IO;

using Mono.Data.Sqlite;
using Ninject;
using Ninject.Modules;

using Mono.BlueZ.DBus;

namespace BluetoothPresence.Configuration
{
	/// <summary>
	/// god class for config
	/// TODO: break this up into something less godlike
	/// </summary>
	public class BluetoothPresenceModule:NinjectModule
	{
		public const string SqliteConnectionStringFormat = "Data Source={0};Version=3;";

		public override void Load ()
		{
			string dataPath = GetExecutingAssemblyFolder();
			DateTime now = DateTime.UtcNow;
			BindDbConnection (dataPath, "presence", now, "presence.db");

			Kernel.Bind<IBluetoothRepository>()
			      .To<BluetoothRepository>()
			      .InTransientScope();
			
			Kernel.Bind<IBluetoothPresenceService>()
				  .To<BluetoothPresenceService>()
				  .InTransientScope()
				  .WithConstructorArgument("adapter", "hci0");

			Kernel.Bind<DBusConnection> ()
			      .ToSelf ()
			      .InSingletonScope ();
		}

		private void BindDbConnection(string dataPath,string bindingName, DateTime now, string nameFormat)
		{
			Kernel.Bind<IDbConnection> ()
			      .ToMethod (c => CreateConnection(dataPath,now,nameFormat))
			      .InSingletonScope ()
			      .Named (bindingName);
		}

		private IDbConnection CreateConnection (string dataPath, DateTime now, string nameFormat)
		{
			string dataFilePath = Path.Combine (dataPath, string.Format (nameFormat, now));

			File.Delete(dataFilePath);

			string connectionString = string.Format (SqliteConnectionStringFormat, dataFilePath);

			IDbConnection connection = null;
			if (File.Exists (dataFilePath)) 
			{
				try 
				{
					connection = new SqliteConnection (connectionString);
				} 
				catch (Exception ex) 
				{
					File.Move (dataFilePath, dataFilePath + string.Format("{0:yyyyMMddhhmmss}",DateTime.UtcNow)+".bad");
				}
			}

			if (connection == null) 
			{
				SqliteConnection.CreateFile (dataFilePath);
				connection = new SqliteConnection(connectionString);
			}

			return connection;
		}

		public static string GetExecutingAssemblyFolder ()
		{
			string exePath = System.Reflection.Assembly.GetExecutingAssembly ().CodeBase;
			if (exePath.StartsWith ("file:"))
			{
				exePath = exePath.Substring (5);
			}
			string exeDir = Path.GetDirectoryName (exePath);
			return exeDir;
		}
	}
}
