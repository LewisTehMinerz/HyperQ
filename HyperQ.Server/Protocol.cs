// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using System.Net.Sockets;

namespace HyperQ.Server
{
    /// <summary>
    /// Contains the commands for the HyperQ protocol.
    /// </summary>
    internal class Protocol
    {
        /// <summary>
        /// Protocol errors.
        /// </summary>
        internal enum Error
        {
            /// <summary>
            /// Command could not be found.
            /// </summary>
            UnknownCommand,
            /// <summary>
            /// There are too many or not enough arguments.
            /// </summary>
            ArgumentMismatch,
            /// <summary>
            /// The queue you tried to dequeue from is empty.
            /// </summary>
            QueueEmpty
        }

        public delegate void Command(Socket sender, List<string> args);

        /// <summary>
        /// Queues an item into a queue.
        /// </summary>
        /// <param name="sender">Sender socket</param>
        /// <param name="args">queue name, data</param>
        public static void Q(Socket sender, List<string> args)
        {
            if (args.Count < 2)
            {
                sender.SendString("ERR " + Error.ArgumentMismatch);
                return;
            }

            var queue = args[0];
            args.RemoveAt(0);

            var data = string.Join(' ', args);

            Queue.Enqueue(queue, data);
            sender.SendString("OK");
        }

        /// <summary>
        /// Dequeues an item from a queue.
        /// </summary>
        /// <param name="sender">Sender socket</param>
        /// <param name="args">queue name</param>
        public static void DQ(Socket sender, List<string> args)
        {
            if (args.Count < 1)
            {
                sender.SendString("ERR " + Error.ArgumentMismatch);
                return;
            }

            var queue = args[0];

            var data = Queue.Dequeue(queue);
            sender.SendString(data ?? "ERR " + Error.QueueEmpty);
        }

        /// <summary>
        /// Clears a queue.
        /// </summary>
        /// <param name="sender">Sender socket</param>
        /// <param name="args">queue name</param>
        public static void CLR(Socket sender, List<string> args)
        {
            if (args.Count < 1)
            {
                sender.SendString("ERR " + Error.ArgumentMismatch);
                return;
            }

            var queue = args[0];

            Queue.ClearQueue(queue);
            sender.SendString("OK");
        }
    }
}
