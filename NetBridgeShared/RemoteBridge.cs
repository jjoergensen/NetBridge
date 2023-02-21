using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace NetBridge.Library
{

	/// <summary>
	/// Launches the host process and tells it to load the target assembly.
	/// </summary>
	public class RemoteBridge
	{
		/// <summary>
		/// Handle process lifecycle combined with a sync object for lock.
		/// </summary>
		public class SyncProcess : IDisposable
		{
			public object sync = new object();
			public Process process = null;

			public void Dispose()
			{
				Shutdown();
			}

			public void Shutdown()
			{
				if (process != null)
				{
					lock (sync)
					{
						var p = process;
						if (p != null)
						{
							if (!p.WaitForExit(100))
							{
								try { p.Kill(); p.Dispose(); } catch { }
							}
						}
					}
				}
			}
		}

		private static readonly Lazy<RemoteBridge> lazy = new Lazy<RemoteBridge>(() => new RemoteBridge());

		public static RemoteBridge Instance { get { return lazy.Value; } }

		/// <summary>
		/// Use the singleton .Instance property to get an instance of this class.
		/// </summary>
		private RemoteBridge()
		{

		}

		// Dictionary with tuple of obj and process
		ConcurrentDictionary<string, SyncProcess> _singletons = new ConcurrentDictionary<string, SyncProcess>();

		// Lock object for the singleton value object. This is used to make sure we don't create two processes at the same time
		// and that we don't create a new process while we're disposing the old one.
		object _syncChangeSingletons = new object();

		private void LaunchProcess(Process clientProcess, string assembly, string codeClass, string isolationToken)
		{
			// Get the path to the current directory
			var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

			// Create a new process
			clientProcess.EnableRaisingEvents = true;

			// Set the command line arguments for the new process
			var processArgs = String.Format("\"{0}\" \"{1}\" \"{2}\"", path + assembly, codeClass, isolationToken);

			// The path to the NetBridge.exe executable
			var target = Path.GetFullPath(path + @"\NetBridge\NetBridge.exe");

			clientProcess.StartInfo = new ProcessStartInfo(target, processArgs);
			clientProcess.StartInfo.UseShellExecute = false; // alternatively both these can be set to true for separate windows
			clientProcess.StartInfo.CreateNoWindow = false;

			clientProcess.Start();

		}

		public static void RestartProcess(string assembly, string codeClass, string isolationToken = "")
		{
			Instance.restartProcess(assembly, codeClass, isolationToken);
		}

		/// <summary>
		/// Restarts a process. It has to exist already for this to have an effect.
		/// </summary>
		/// <param name="assembly"></param>
		/// <param name="codeClass"></param>
		private void restartProcess(string assembly, string codeClass, string isolationToken = "")
		{
			var key = String.Concat(assembly, codeClass, isolationToken);
			SyncProcess singleton = null;
			if (_singletons.TryGetValue(key, out singleton))
			{
				var lockObj = singleton.sync;
				lock (lockObj)
				{
					var existing = singleton.process;
					try
					{
						var hasExited = false;
						try
						{
							hasExited = existing.HasExited;
						}
						catch // can throw if the process has been disposed
						{
							hasExited = true;
						}

						if (!hasExited)
						{
							try
							{
								existing.Kill();
							}
							catch { }
						}
						existing.Dispose();
					}
					catch { }

					var newProcess = new Process();
					LaunchProcess(newProcess, assembly, codeClass, isolationToken);

					singleton.process = newProcess;
				}
			}
		}

		public static SyncProcess EnsureRemoteConnection(string assembly, string codeClass, string isolationToken = "")
		{
			return Instance._ensureRemoteConnection(assembly, codeClass, isolationToken);
		}

		/// <summary>
		/// Configures a process server with the given assembly and class name.
		/// The reason for this is to be able to have multiple types of service processes using the same bridge process.
		/// If a server is likely to crash, you need to make sure, it is separated from other process servers.
		/// </summary>
		/// <param name="assembly">Assembly containing the server</param>
		/// <param name="codeClass">Class to call within that server. This class needs a method named "GetGoing"</param>
		/// <returns></returns>
		// This is the code that will be used to start the other process
		private SyncProcess _ensureRemoteConnection(string assembly, string codeClass, string isolationToken = "")
		{

			var key = String.Concat(assembly, codeClass, isolationToken);
			SyncProcess singleton = null;
			if (!_singletons.TryGetValue(key, out singleton))
			{
				Process process = null;
				object obj = new object();
				lock (_syncChangeSingletons)
				{
					// We need to get it again after getting the lock, so we don't
					// risk two processes creating this simul.
					_singletons.TryGetValue(key, out singleton);
					process = new Process();
					singleton = new SyncProcess() { sync = obj, process = process };
					// safety check, this will always succeed
					if (!_singletons.TryAdd(key, singleton))
						throw new Exception("locking logic is violated");
				}

				lock (obj)
				{
					LaunchProcess(process, assembly, codeClass, isolationToken);
				}
			}

			lock (singleton.sync)
			{
				var hasExited = false;
				try
				{
					hasExited = singleton.process.HasExited;
				}
				catch // can throw if the process has been disposed
				{
					hasExited = true;
				}
				if (hasExited)
				{
					RestartProcess(assembly, codeClass);
				}
			}

			return singleton;
		}
	}
}
