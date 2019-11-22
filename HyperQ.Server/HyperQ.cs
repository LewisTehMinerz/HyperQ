using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using CommandLine;
using Serilog;
using Serilog.Events;

namespace HyperQ.Server
{
    /// <summary>
    /// The main class for the server.
    /// </summary>
    internal static class HyperQ
    {
        // terminator used to end sockets
        private const string Terminator = "\r\n";

        // options class for CommandLine to use
        internal class Options
        {
            [Option('v', "verbose", Required = false, HelpText = "Shows debug output in console.")]
            public bool Verbose { get; set; }

            [Option('p', "port", Default = 5000, Required = false, HelpText = "Runs HyperQ.Server on a specific port.")]
            public int Port { get; set; }
        }

        // "false" entry point, only parses options and then calls
        // the "true" entry point
        private static void Main(string[] args)
        {
            // can't name a already named thread
            if (Thread.CurrentThread.Name == null)
            {
                // set thread name for logging purposes
                Thread.CurrentThread.Name = "MainThread";
            }

            // parse options
            Parser.Default.ParseArguments<Options>(args)
                // run HyperQ once parsed
                .WithParsed(Run);
        }

        // "true" entry point
        private static void Run(Options opts)
        {
            // template for serilog, same for the file loggers as
            // they are rolling and don't need a full timestamp since
            // it is in the file name (yyyyMMdd)
            const string outputTemplate =
                "[{Timestamp:HH:mm:ss} {Level:u3} {ThreadName}::{ThreadId}] {Message:lj}{NewLine}{Exception}";

            // globally set the Serilog logger, since this is used
            // application-wide
            Log.Logger = new LoggerConfiguration()
                // makes the global minimum level verbose
                .MinimumLevel.Verbose()
                // add thread data to logs
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .WriteTo.Console(
                    // if verbose then minimum = verbose else minimum = info
                    opts.Verbose
                        ? LogEventLevel.Verbose
                        : LogEventLevel.Information,
                    outputTemplate)
                // serilog adds the timestamp onto the end of the file name,
                // so we put a hyphen there for clarity
                .WriteTo.File("logs\\hyperq-.log",
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    outputTemplate: outputTemplate)
                .WriteTo.File("logs\\hyperq-verbose-.log",
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: LogEventLevel.Verbose,
                    outputTemplate: outputTemplate)
                .CreateLogger();

            Log.Information("HyperQ.Server {Version} starting.",
                FileVersionInfo.GetVersionInfo(
                        Assembly.GetExecutingAssembly().Location)
                    .ProductVersion);
            Log.Verbose("Provided options class: {@Options}", opts);

            // HyperQ runs here
            try
            {
                // sanity checks for options here
                if (opts.Port < 1 || opts.Port > 65535)
                {
                    Log.Error("Port {Port} is out of range. It must be 1-65535.", opts.Port);
                    Environment.Exit(1);
                }

                // start HyperQ
                Log.Information("Starting socket server on port {Port}.", opts.Port);

                // bind to all interfaces
                var localEndpoint = new IPEndPoint(IPAddress.Any, opts.Port);

                Log.Verbose("{@Endpoint}", localEndpoint);

                // create the socket
                var listener = new Socket(
                    SocketType.Stream,
                    ProtocolType.Tcp);

                // bind to the local endpoint
                listener.Bind(localEndpoint);

                // start listening and create a connection queue
                // of 5 clients before we start dropping them
                listener.Listen(5);

                Log.Information("Listening on port {Port}. Waiting for connections.", opts.Port);

                while (true)
                {
                    var client = listener.Accept();

                    var address = (client.RemoteEndPoint as IPEndPoint)?.Address;

                    Log.Debug("Connection from {IP}", address);
                    Log.Verbose("starting thread to handle {IP}", address);

                    var clientHandler = new Thread(HandleClient)
                    {
                        Name = "Handler[" + address + "]"
                    };

                    clientHandler.Start(client);
                }

                // HyperQ was terminated by the user most likely at this point.
            }
            catch (Exception e)
            {
                // Log error to console as fatal
                Log.Fatal(e,
                    "An exception worked its way back up to the entry point of HyperQ. This should never happen. Will now exit.");
                // Exit with a failure code
                Environment.Exit(-1);
            } finally
            {
                Log.Information("Goodbye!");
                // Cleanup before exit
                Log.CloseAndFlush();
            }
        }

        // client handler
        private static void HandleClient(object clientObj)
        {
            try
            {
                // should never be null but sanity check anyway
                if (!(clientObj is Socket client)) throw new ArgumentNullException(nameof(client));

                Log.Verbose("thread started, handling commands for this client");

                // command loop
                while (true)
                {
                    // prepare for next read
                    var commandData = new byte[65536];
                    var command = "";
                    while (true)
                    {
                        try
                        {
                            // read data from client
                            var bytes = client.Receive(commandData);

                            // append to command
                            var newData = Encoding.ASCII.GetString(commandData, 0, bytes);
                            Log.Verbose("<< {Data}", newData);
                            command += newData;

                            // includes terminator at end, end of command
                            if (command.EndsWith(Terminator))
                                break;
                        }
                        catch (SocketException e)
                        {
                            // disconnected?
                            if (e.SocketErrorCode == SocketError.ConnectionReset)
                            {
                                Log.Verbose("client disconnected, stopping thread");
                                throw new Exception("abort");
                            }
                        }
                    }

                    command = command.Remove(command.IndexOf(Terminator, StringComparison.Ordinal));

                    Log.Debug("< {Cmd}", command);

                    var args = command.Split(' ').ToList();
                    var cmd = args[0];
                    args.RemoveAt(0);

                    args = args.Where(x => !string.IsNullOrEmpty(x)).ToList();

                    try
                    {
                        // disabled below because handled by catch
                        // ReSharper disable once PossibleNullReferenceException
                        typeof(Protocol).GetMethod(cmd).CreateDelegate(typeof(Protocol.Command))
                            .DynamicInvoke(client, args);
                    }
                    catch (NullReferenceException)
                    {
                        Log.Verbose("no command called {Cmd}", cmd);
                        client.SendString("ERR " + Protocol.Error.UnknownCommand);
                    }
                }
            }
            catch (Exception e)
            {
                if (e.Message != "abort")
                {
                    Log.Error(e, "Client handler throw an error:");

                    if (clientObj is Socket client && client.Connected)
                        client.Disconnect(true);
                }
            }
        }

        // extension method for ease of use
        public static void SendString(this Socket client, string data)
        {
            Log.Verbose("> {Data}", data);
            client.Send(Encoding.ASCII.GetBytes(data + Terminator));
        }
    }
}
