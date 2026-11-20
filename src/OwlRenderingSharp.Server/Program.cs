using OwlRenderingSharp.Server;

// A render server over standard input and output: see Protocol.cs for the commands.
using Server server = new(Console.OpenStandardInput(), Console.OpenStandardOutput());
return server.Run();
