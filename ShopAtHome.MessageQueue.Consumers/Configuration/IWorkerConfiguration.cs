namespace ShopAtHome.MessageQueue.Consumers.Configuration
{
    /// <summary>
    /// A configuration for a base worker consumer
    /// </summary>
    public interface IWorkerConfiguration : IActorConfiguration
    {
        /// <summary>
        /// The queue identifier (if any) to which data should be written after the worker finishes performing its job on a message
        /// </summary>
        string NextQueue { get; }
    }
}
