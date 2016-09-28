using System;
using ShopAtHome.MessageQueue.Consumers.Configuration;

namespace ShopAtHome.MessageQueue.Consumers
{
    /// <summary>
    /// Basic actor interface
    /// </summary>
    public interface IActor
    {
        /// <summary>
        /// Sets initial state
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        IActor Configure(IActorConfiguration configuration);

        /// <summary>
        /// What the actor does
        /// </summary>
        void Work();

        /// <summary>
        /// Deactivates the actor
        /// </summary>
        void TurnOff();

        /// <summary>
        /// The unique identifier of the actor
        /// </summary>
        Guid Id { get; }
    }
}
