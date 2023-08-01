using ExampleNetLibrary.Services;
using NetBridge.Library;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ExampleNetLibrary.Services.Greeter;

namespace FrameworkApplication
{

    /// <summary>
    /// This is a demo app, that you can modify to fit your needs. It is a .NET 4.6.2 Framework that calls a .NET 6 library
    /// </summary>
    internal partial class Program
	{
		static void Main(string[] args)
		{
			CallHelloWorld();
		}

		private static void CallHelloWorld()
		{
			#region Configure to .NET Core assembly
			// Assembly is copied as a post-build event on the library we want to call
			// It is copied separately from the main app, as DLLs cannot mix.
			var assemblyToLoad = @"\NetBridge\ExampleNetLibrary.dll";
			var typeToLoad = "ExampleNetLibrary.ExampleServer";
			#endregion

			for (int i = 0; i < 10000; i++)
			{
				// Load the host process and target assembly
				var r = new RemoteCaller<GreeterClient>(assemblyToLoad, typeToLoad);

				// Call the method
				var resp = r.Call(x => x.SayHelloAsync(new HelloRequest { Name = "World" }));

				// Wait for the response
				var res = resp.GetAwaiter().GetResult();

				Console.WriteLine(res);

				//Thread.Sleep(1000);

				// r.Kill();

				if (i % 100 == 0)
				{
					Console.Write(".");
				}
			}


			Console.WriteLine("Press any key to exit...");
			Console.ReadKey();
		}
	}
}
