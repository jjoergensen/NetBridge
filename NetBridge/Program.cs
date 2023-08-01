using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// DO NOT MODIFY THIS CLASS

namespace NetBridge.Host
{
	/// <summary>
	/// Necessary host for invoking .NET 6+ class from .NET 4.5+.
	/// The purpose is to ease the transition from .NET 4.5 to .NET 6,
	/// by allowing implementations in .NET 6 to be invoked from .NET 4.5.
	/// Difficult-to-migrate components should take priority first in this migration/rewrite.
	/// </summary>
	internal class Program
	{
		/// <summary>
		/// A post-build event will copy this exe to the output folder. This is the host process for
		///  invoking the .NET class of the same framework as the target class. The target class
		/// will be passed in through the args of this program. The post-build event can be set on the
		/// library that refers to this project or on both projects.
		/// </summary>
		/// <param name="args">Array where arg[0] is assembly and arg[1] is class type. Class needs to implement a "RunServer" method.</param>
		static void Main(string[] args)
		{
			// To set this up, add the following to the post-build event on your .NET 6+ project (replace FWApp with actual app):
			// if not exist "$(ProjectDir)..\FWApp\bin\Debug\NetBridge" mkdir "$(ProjectDir)..\FWApp\bin\Debug\NetBridge"
			// xcopy "$(TargetDir)*" "$(ProjectDir)..\FWApp\bin\Debug\NetBridge" / Y


			// We will start the server.
			// The content of this server, is defined by the outside caller. So that we could write it
			// somewhere else, and have it read the Assembly to load from a arg passed into this program.

			// This would make this a simple program, that we compile once and just reuse.

			var assemblyToLoad = args[0];
			var typeToLoad = args[1];
			var uniqueName = args[2];

			// Instantiate

			var assembly = Assembly.LoadFrom(assemblyToLoad);
			var instanceType = assembly.GetType(typeToLoad);
			if (instanceType is null)
			{
				throw new Exception("Could not find type " + typeToLoad);
			}

			var instance = Activator.CreateInstance(instanceType);

			var stopEvent = new AutoResetEvent(false);

			// call a method on the instance
			var method = instanceType.GetMethod("Run");
			if (method is null)
				throw new Exception("Could not find method RunServer on type " + typeToLoad);

			Action action = () => {
				stopEvent.Set();
			};


			// invoke the method and pass the exit-delegate as a parameter
			var _ = method.Invoke(instance, new object[] { action, uniqueName });

			stopEvent.WaitOne();
		}
	}
}
