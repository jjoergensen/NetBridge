using GrpcDotNetNamedPipes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetBridge.Library
{
	public abstract class NetBridgeServer
	{
		public abstract void Binding(NamedPipeServer server, Action exitAction);

		private static int GetCustomHashCode(string input)
		{
			int hash = 5381;

			for (int i = 0; i < input.Length; i++)
			{
				hash = ((hash << 5) + hash) + input[i];
			}

			return hash;
		}


		/// <summary>
		/// The main entry point for the application. The Run is by convention the method
		/// name that will be invoked and the server will be started.
		/// </summary>
		/// <param name="exit"></param>
		public virtual void Run(Action exit, string isolationToken = "")
		{
			var stopEvent = new AutoResetEvent(false);

			var assembly = this.GetType().Assembly; // Not Current Assembly
			var assemblyFilename = Path.GetFileName(assembly.Location);
			var assemblyHash = GetCustomHashCode(assemblyFilename);

			var serverName = string.Concat("NETBRIDGE_", assemblyHash, "_", isolationToken);
			var server = new NamedPipeServer(serverName);

			Action exitAction = () =>
			{
				try
				{
					server.Kill();
				}
				catch { }
				server.Dispose();
				stopEvent.Set();
			};

			// Bind services
			Binding(server, exitAction);
			// Start the server offering above services. 
			server.Start();

			// Wait for the server to stop
			stopEvent.WaitOne();

			if (exit != null)
			{
				exit();
			}
		}
	}
}
