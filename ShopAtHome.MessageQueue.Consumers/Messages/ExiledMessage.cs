using System;

namespace ShopAtHome.MessageQueue.Consumers.Messages
{
    /// <summary>
    /// If a message causes sufficient worker crashes when we try to process it, we need to kick it out of the queue
    /// and examine it to figure out what's wrong. This class holds the information that's useful in analyzing such messages.
    /// </summary>
    public class ExiledMessage
    {
        /// <summary>
        /// The message that is being exiled
        /// </summary>
        public Message<object> Message { get; set; } 

        /// <summary>
        /// The error that the exception has been causing
        /// </summary>
        public Exception Error { get; set; }

        /// <summary>
        /// The identifier of the queue from which the message comes
        /// </summary>
        public string SourceQueueIdentifier { get; set; }

        /// <summary>
        /// The moment in time at which this message was exiled
        /// </summary>
        public DateTime ExileEventDateTime { get; set; }
    }
}
