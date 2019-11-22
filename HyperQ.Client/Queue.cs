using System.Net.Sockets;

namespace HyperQ.Client
{
    /// <summary>
    /// A queue object on the client.
    /// </summary>
    public class Queue
    {
        /// <summary>
        /// The name of the queue.
        /// </summary>
        private string _name;
        /// <summary>
        /// The socket to communicate with the server.
        /// </summary>
        private Socket _socket;

        public Queue(string name, Socket socket)
        {
            _name = name;
            _socket = socket;
        }

        /// <summary>
        /// Creates a subqueue. Not strictly required to be used but just helps with organization.
        /// </summary>
        /// <param name="name">The name of the subqueue.</param>
        /// <returns>The subqueue.</returns>
        public Queue SubQueue(string name)
        {
            return new Queue($"{_name}:{name}", _socket);
        }

        /// <summary>
        /// Queues data.
        /// </summary>
        /// <param name="data">The data to queue.</param>
        public void QueueData(string data)
        {
            HyperQ.SendString(_socket, $"Q {_name} {data}");
        }

        /// <summary>
        /// Dequeues data.
        /// </summary>
        /// <returns></returns>
        public bool TryDequeue(out string data)
        {

        }
    }
}
