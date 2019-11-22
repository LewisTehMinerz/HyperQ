using System.Collections.Generic;
using Serilog;

namespace HyperQ.Server
{
    /// <summary>
    /// Utility class for storing data in queues.
    /// </summary>
    internal class Queue
    {
        // dictionary for storing queues by name
        private static readonly Dictionary<string, Queue<string>> Queues = new Dictionary<string, Queue<string>>();

        // locks for safely enqueuing and dequeuing data
        private static readonly object QueueLock = new object();

        /// <summary>
        /// Initializes a queue if it doesn't exist.
        /// </summary>
        /// <param name="queueName">The name of the queue.</param>
        private static void InitQueue(string queueName)
        {
            if (Queues.ContainsKey(queueName)) return;

            Log.Verbose("queue operation is making a new queue");
            Log.Debug("creating new queue '{Queue}'", queueName);
            Queues.Add(queueName, new Queue<string>());
        }

        /// <summary>
        /// Adds data to a queue.
        /// </summary>
        /// <param name="queueName">The name of the queue.</param>
        /// <param name="value">The data.</param>
        public static void Enqueue(string queueName, string value)
        {
            Log.Verbose("waiting for queue lock");
            lock (QueueLock)
            {
                InitQueue(queueName);

                Log.Verbose("queuing data {Data} into queue '{Queue}'", value, queueName);
                Queues[queueName].Enqueue(value);
            }
        }

        /// <summary>
        /// Attempts to get data from a queue.
        /// </summary>
        /// <param name="queueName">The name of the queue.</param>
        /// <returns>Data or null.</returns>
        public static string Dequeue(string queueName)
        {
            Log.Verbose("waiting for queue lock");
            lock (QueueLock)
            {
                InitQueue(queueName);

                Log.Verbose("attempting to dequeue data from queue '{Queue}'", queueName);
                return Queues[queueName].TryDequeue(out var data) ? data : null;
            }
        }

        /// <summary>
        /// Clears a queue.
        /// </summary>
        /// <param name="queueName">The name of the queue.</param>
        public static void ClearQueue(string queueName)
        {
            Log.Verbose("waiting for queue lock");
            lock (QueueLock)
            {
                InitQueue(queueName);

                Log.Verbose("clearing queue '{Queue}'", queueName);
                Queues[queueName].Clear();
            }
        }
    }
}
