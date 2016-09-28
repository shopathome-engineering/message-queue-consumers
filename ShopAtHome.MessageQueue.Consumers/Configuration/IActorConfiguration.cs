using System;

namespace ShopAtHome.MessageQueue.Consumers.Configuration
{
    /// <summary>
    /// Base configuration interface for any consumer class
    /// </summary>
    public interface IActorConfiguration
    {
        /// <summary>
        /// The type of the actor - used for instantiation by controller applications
        /// </summary>
        Type ActorType { get; }

        /// <summary>
        /// The identifier of the queue from which the actor reads its data
        /// </summary>
        string SourceQueue { get; }

        /// <summary>
        /// The identifier of the queue to which this actor sends reports about its state
        /// </summary>
        string ReportQueue { get; set; }
    }
}
