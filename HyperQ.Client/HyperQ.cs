using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HyperQ.Client
{
    /// <summary>
    /// Main class for connecting to a HyperQ server.
    /// </summary>
    public class HyperQ
    {
        // terminator used to end socket messages
        internal const string Terminator = "\r\n";

        /// <summary>
        /// The socket for communicating with the server.
        /// </summary>
        private readonly Socket _socket;

        /// <summary>
        /// Establishes a connection to the HyperQ server.
        /// </summary>
        /// <param name="endpoint">The IPEndPoint to use when establishing a socket connection.</param>
        public HyperQ(IPEndPoint endpoint)
        {
            _socket = new Socket(
                SocketType.Stream,
                ProtocolType.Tcp);

            _socket.Connect(endpoint);
        }

        /// <summary>
        /// Establishes a connection to the HyperQ server.
        /// </summary>
        /// <param name="address">The IPAddress the server is running on.</param>
        /// <param name="port">The port the server is running on.</param>
        public HyperQ(IPAddress address, int port)
        {
            if (port > 65535 || port < 1)
                throw new ArgumentOutOfRangeException(nameof(port));

            _socket = new Socket(
                SocketType.Stream,
                ProtocolType.Tcp);

            _socket.Connect(address, port);
        }

        /// <summary>
        /// Gets a queue.
        /// </summary>
        /// <param name="name">The name of the queue.</param>
        /// <returns>The queue.</returns>
        public Queue Queue(string name)
        {
            return new Queue(name, _socket);
        }

        // extension method for ease of use
        internal static void SendString(Socket client, string data)
        {
            client.Send(Encoding.ASCII.GetBytes(data + Terminator));
        }
    }
}
