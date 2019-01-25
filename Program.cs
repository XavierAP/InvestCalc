using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

using JP.SQLite;

namespace JP.InvestCalc
{
	internal static class Program
	{
		public const string AppName = "InvestCalc";

		private readonly static SQLiteConnector dataConn = null;

		static Program()
		{
			string dataFolder = GetDataFolder();
			string dataFile = Path.Combine(dataFolder, "Data.DB");

			try
			{
				if(File.Exists(dataFile))
					dataConn = new SQLiteConnector(dataFile, true);
				else
				{
					const string ddlFile = "Data_definition.sql"; // SQL script file that creates an empty database.
					if(!File.Exists(ddlFile))
					{
						MessageBox.Show("Application file missing!\n" + ddlFile,
							AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}
					Directory.CreateDirectory(dataFolder); // does nothing if already existing.
					dataConn = new SQLiteConnector(dataFile, false);
					dataConn.Write(File.ReadAllText(ddlFile));
				}
			}
			catch(Exception err)
			{
				MessageBox.Show($"Error creating database file!\n\n{err.GetType().Name}\n{err.Message}",
					AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);

				dataConn = null; // to trigger early return from Main().
				return;
			}
			Debug.Assert(dataConn != null && File.Exists(dataFile));
		}

		/// <summary>Application entry point:
		/// runs a FormMain window.</summary>
		[STAThread]
		static void Main()
		{
			if(dataConn == null) return; // Error message already shown from static constructor.

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
#if DEBUG
			Application.Run(new FormMain(new Data(dataConn)));
#else
			try { Application.Run(new FormMain(data)); }
			catch(Exception err)
			{
				MessageBox.Show($"{err.GetType().Name}\n{err.Message}",
					appName, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
#endif
		}

		private static string GetDataFolder()
		{
			string folder = Properties.Settings.Default.dataFolder;

			if(string.IsNullOrWhiteSpace(folder))
				return GetDataFolderDefault();
			
			try { Directory.CreateDirectory(folder); }
			catch(Exception err)
			{
				MessageBox.Show($"Error creating custom data folder:\n{folder}\nDefaulting to %AppData%.\n\n{err.GetType().Name}\n{err.Message}",
					AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return GetDataFolderDefault();
			}

			Debug.Assert(Directory.Exists(folder));
			return folder;
		}
		private static string GetDataFolderDefault() => Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"JP", AppName);
	}
}
