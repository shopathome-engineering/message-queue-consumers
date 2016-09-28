namespace ShopAtHome.MessageQueue.Consumers.Messages
{
    /// <summary>
    /// A report from a listener
    /// </summary>
    public class ListenerReport
    {
        /// <summary>
        /// How many messages were waiting in the queue at the time the report was sent
        /// </summary>
        public int MessageCount { get; set; }

        /// <summary>
        /// The identifier of the queue in question
        /// </summary>
        public string Queue { get; set; }
    }
}
