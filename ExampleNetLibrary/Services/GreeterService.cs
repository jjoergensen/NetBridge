using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ExampleNetLibrary;
using Microsoft.Extensions.Logging;

namespace ExampleNetLibrary.Services
{
    public class GreeterService : Greeter.GreeterBase
    {
		private readonly Action _exitAction;
		public GreeterService(Action exitAction)
        {
			_exitAction = exitAction;

		}

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.Name
            });
        }

		public override Task<Empty> Shutdown(Empty request, ServerCallContext context)
		{
			if (_exitAction != null)
			{
				_exitAction();
			}
			return Task.FromResult(new Empty());	
		}
	}
}
