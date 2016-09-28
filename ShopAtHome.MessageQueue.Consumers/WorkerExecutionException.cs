using System;

namespace ShopAtHome.MessageQueue.Consumers
{
    public class WorkerExecutionException : Exception
    {
        public WorkerExecutionException(Exception error, object message) : base(error.Message, error)
        {

        }

        /// <summary>
        /// A unique fingerprint of the message that was being worked on when the exception occurred
        /// </summary>
        public byte[] MessageHash { get; set; }
    }
}
