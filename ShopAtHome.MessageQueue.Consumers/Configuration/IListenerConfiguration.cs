namespace ShopAtHome.MessageQueue.Consumers.Configuration
{
    /// <summary>
    /// Configuration that is specific to a listener/poller/monitor
    /// </summary>
    public interface IListenerConfiguration : IActorConfiguration
    {
        /// <summary>
        /// The interval the listener should use in polling
        /// Units are seconds
        /// </summary>
        int PollingInterval { get; set; }

        /// <summary>
        /// The interval at which the listener will write a report if any messages exist, regardless of its message count threshold
        /// Units are seconds
        /// </summary>
        int QueueReadInterval { get; set; }

        /// <summary>
        /// The number of messages at which the listener will write a report
        /// </summary>
        int QueueReadMessageCountThreshold { get; set; }
    }
}
