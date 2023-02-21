using Grpc.Core;
using GrpcDotNetNamedPipes;
using System;
using System.IO;


namespace NetBridge.Library
{
	public class RemoteCaller<T> : ClientBase where T : ClientBase<T>
	{
		T _client = null;
		string _assemblyToLoad;
		string _typeToLoad;
		string _isolationToken;


		/// <summary>
		/// There is a difference in GetHashCode between .NET framework and .NET core,
		/// so I use this instead.
		/// </summary>
		private static int GetCustomHashCode(string input)
		{
			int hash = 5381;

			for (int i = 0; i < input.Length; i++)
			{
				hash = (hash << 5) + hash + input[i];
			}

			return hash;
		}

		public RemoteCaller(string assemblyToLoad, string typeToLoad, string isolationToken="")
		{
			_assemblyToLoad = assemblyToLoad;
			_typeToLoad = typeToLoad;
			_isolationToken = isolationToken;

			var assemblyFilename = Path.GetFileName(assemblyToLoad);
			var assemblyHash = GetCustomHashCode(assemblyFilename);
			var serverName = string.Concat("NETBRIDGE_", assemblyHash, "_", isolationToken);
			var channel = new NamedPipeChannel(".", serverName, new NamedPipeChannelOptions() { ConnectionTimeout = 1000 });

			// Create a new instance of the client.
			_client = (T)Activator.CreateInstance(typeof(T), channel);
		}

		/// <summary>
		/// Call the function and get the result. If the remote connection is broken, we will automatically try to restart the process and reconnect. But it may fail if the process is still shutting down.
		/// </summary>
		public TResult Call<TResult>(Func<T, TResult> func)
		{
			RemoteBridge.EnsureRemoteConnection(_assemblyToLoad, _typeToLoad, _isolationToken);

			TResult result;
			try
			{
				result = func(_client);
			}
			catch (RpcException ex)
			{
				// if pipe is broken (this can happen for ex. if you close it from the server side and quickly make a request).
				if (ex.StatusCode == StatusCode.Unavailable || ex.StatusCode == StatusCode.Unavailable)
				{
					//RemoteBridge.RestartProcess(_assemblyToLoad, _typeToLoad);
					RemoteBridge.EnsureRemoteConnection(_assemblyToLoad, _typeToLoad, _isolationToken);
					try
					{
						result = func(_client);
						return result;
					}
					catch
					{;
						throw;
					};
				}
				throw;
			}
			catch
			{
				throw;
			}
			return result;
		}

		/// <summary>
		/// Kills the client process. This is useful if you want to restart the process.
		/// But it will not restart the remote process, so that may linger around unless
		/// you have implemented a separate way to kill it. One way is to include a Shutdown
		/// command in your remote interface, and call that before you call Kill().
		/// </summary>
		public void Kill()
		{
			RemoteBridge.RestartProcess(_assemblyToLoad, _typeToLoad, _isolationToken);
		}
	}
}
