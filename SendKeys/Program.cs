using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;

namespace SendKeys
{
	static class Program
	{
		[DllImport("User32.dll")]
		static extern int SetForegroundWindow(IntPtr point);

		[DllImport("kernel32.dll")]
		static extern bool AttachConsole(int dwProcessId);
		private const int ATTACH_PARENT_PROCESS = -1;

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool FreeConsole();

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			int pid = -1;
			int wait = 0;
			int file = 0;
			string line;
			bool filepresent = false;
			string filetoread = "";
			string keysToSend = "";

			var validArguments = args?.Length == 2 || args?.Length == 3 || args?.Length == 4;

			if (validArguments)
			{
				for (int i = 0; i < args.Length; i++)
				{
					int pidIndex = args[i].IndexOf("pid:", StringComparison.OrdinalIgnoreCase);
					int waitIndex = args[i].IndexOf("wait:", StringComparison.OrdinalIgnoreCase);
					int fileIndex = args[i].IndexOf("file:", StringComparison.OrdinalIgnoreCase);
					if (pidIndex > -1)
					{
						var pidString = args[i].Substring(pidIndex + "pid:".Length);
						int.TryParse(pidString, out pid);
					}
					else if (waitIndex > -1)
					{
						var waitString = args[i].Substring(waitIndex + "wait:".Length);
						int.TryParse(waitString, out wait);
					}
					else if (fileIndex > -1)
					{
						var fileString = args[i].Substring(waitIndex + "file:".Length + 2);
						int.TryParse(fileString, out file);
						filepresent = true;
						filetoread = fileString;
					}
					else
					{
						keysToSend = args[i].Replace("'", "\"");
					}
				}
			}

			if (!validArguments)
			{
				WriteError("Invalid arguments. Please define a process id and the string value to send as keys." +
					"\n  Example:  SendKeys.exe -pid:4711 \"Keys to send{Enter}\"" +
					"\n  Optional: Add -wait:100 to add a delay of 100 milliseconds, for example." +
					"\n  Optional: Add -file:'test.txt' to read the contents of a file.");

				return;
			}

			Process process = null;
			try
			{
				process = Process.GetProcessById(pid);
			}
			catch (Exception ex)
			{
				WriteError(ex.ToString());
				return;
			}

			if (process.MainWindowHandle == IntPtr.Zero)
			{
				WriteError($"Process {process.ProcessName} ({process.Id}) has no main window handle.");
			}
			else
			{
				if (wait > 0)
					Thread.Sleep(wait);
				if (filepresent)
				{
					SetForegroundWindow(process.MainWindowHandle);
					try
					{
						StreamReader sr = new StreamReader(filetoread);
						int linenumber = 0;
						line = sr.ReadLine();
						while (sr.Peek() > -1)
						{
							SetForegroundWindow(process.MainWindowHandle);
							string keytosendline = line.Replace("+", "{+}");
							System.Windows.Forms.SendKeys.SendWait(keytosendline + '\n');
							WriteInfo("On line: " + linenumber.ToString());

							linenumber = linenumber + 1;
							line = sr.ReadLine();
						}
						sr.Close();
					}
					catch (Exception e)
					{
						WriteError("Exception: " + e.Message);

					}

				}
				else
				{
					SetForegroundWindow(process.MainWindowHandle);
					System.Windows.Forms.SendKeys.SendWait(keysToSend);
				}
			}
		}

		private static void WriteError(string message)
		{
			AttachConsole(ATTACH_PARENT_PROCESS);
			Console.WriteLine(message);
			FreeConsole();
		}

		public static void WriteInfo(string message)
		{
			AttachConsole(ATTACH_PARENT_PROCESS);
			Console.WriteLine(message);
			FreeConsole();
		}
	}
}
