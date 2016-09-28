using System;

namespace ShopAtHome.MessageQueue.Consumers
{
    /// <summary>
    /// This listener implementation enforces that only one instance of a worker can exist for the queue it's monitoring
    /// </summary>
    /// <typeparam name="TKeyType"></typeparam>
    public abstract class SoloListener<TKeyType> : Listener
    {
        private readonly TKeyType _key;
        protected SoloListener(TKeyType key, IConnectionProvider factory) : base(factory)
        {
            _key = key;
        }

        /// <summary>
        /// This implementation checks to see if the worker for the monitor's key type has an existing instance, otherwise it follows the same logic
        /// as the base listener class
        /// </summary>
        /// <param name="messageCount"></param>
        /// <param name="timeSinceLastReport"></param>
        /// <returns></returns>
        protected override bool CreateWorkerCriteraIsMet(int messageCount, TimeSpan timeSinceLastReport)
        {
            return !SoloQueueWorker<TKeyType>.InstanceMap.ContainsKey(_key) && base.CreateWorkerCriteraIsMet(messageCount, timeSinceLastReport);
        }
    }
}
