# NetBridge

NetBridge is a .NET component that enables calling between different .NET environments and processes using gRPC and named pipes. It provides an easy way to achieve process isolation, connecting different versions of .NET Framework and .NET Core (.NET 6+), or calling between different processes within the same .NET environment.

## Why use NetBridge?

NetBridge is a helpful tool if you are using an older version of .NET and want to migrate parts of your application without migrating everything.
It also enables you to isolate code in a process from the rest of your application.

You could also just use the gRPC library from cyanfish directly (mentioned in credits), but NetBridge provides a simple interface to call into a gRPC service.

## How to use NetBridge

Included in the repository is an example of a .NET Framework application called "FrameworkApplication" that calls into a gRPC service included in ExampleNetLibrary.

```csharp
// Load the host process and target assembly
var r = new RemoteCaller<GreeterClient>(assemblyToLoad, typeToLoad);

// Call the method
var resp = r.Call(x => x.SayHelloAsync(new HelloRequest { Name = "World" }));

// Wait for the response
var res = resp.GetAwaiter().GetResult();

```
By running the code, it should output a 'Hello World' response.

## Requirements
To use the gRPC services library with NetBridge, you must include a post-build event that copies the binaries to a sub-directory of the application you intend to run.
```
if not exist "$(SolutionDir)FrameworkApplication\bin\$(Configuration)\NetBridge" mkdir "$(SolutionDir)FrameworkApplication\bin\$(Configuration)\NetBridge"
xcopy "$(TargetDir)*" "$(SolutionDir)FrameworkApplication\bin\$(Configuration)\NetBridge" /Y
```

It's also helpful to include other DLL binaries by adding the following to the csproj file:
```xml
<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
```

## Credits
NetBridge uses the grpc-dotnet-namedpipes library to support gRPC calls over named pipes. You can find more information about grpc-dotnet-namedpipes at https://github.com/cyanfish/grpc-dotnet-namedpipes/
