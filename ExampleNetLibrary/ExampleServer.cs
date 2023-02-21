using Google.Protobuf.WellKnownTypes;
using GrpcDotNetNamedPipes;
using ExampleNetLibrary;
using NetBridge;
using System.IO.Pipes;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using NetBridge.Library;
using Grpc.Core;
using ExampleNetLibrary.Services;

namespace ExampleNetLibrary
{
	public class ExampleServer : NetBridgeServer
	{
		public override void Binding(NamedPipeServer server, Action exitAction)
		{
			Greeter.BindService(server.ServiceBinder, new GreeterService(exitAction));
		}

        public override void Run(Action exit, string isolationToken = "")
        {
            base.Run(exit, isolationToken);
        }
    }
}
