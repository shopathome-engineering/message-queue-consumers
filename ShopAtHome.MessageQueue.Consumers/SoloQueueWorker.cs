using System;
using System.Collections.Concurrent;
using ShopAtHome.MessageQueue.Statistics;

namespace ShopAtHome.MessageQueue.Consumers
{
    /// <summary>
    /// Designed to enforce a single worker per provided key
    /// </summary>
    public abstract class SoloQueueWorker<TKeyType> : BaseWorker
    {
        /// <summary>
        /// Keeps track of instances of this class per KeyType value
        /// </summary>
        public static readonly ConcurrentDictionary<TKeyType, SoloQueueWorker<TKeyType>> InstanceMap = new ConcurrentDictionary<TKeyType, SoloQueueWorker<TKeyType>>();

        /// <summary>
        /// The value of the unique key to which this worker is assigned
        /// </summary>
        protected TKeyType Key { get; }

        /// <summary>
        /// Returns a string that is the unique combination of this worker type and its assigned key value
        /// </summary>
        /// <returns></returns>
        public string GenerateQueueName()
        {
            return GetType().Name + "_" + Key;
        }

        /// <summary>
        /// If an instance exists for the provided key, deactivates it and removes it from the instance map
        /// </summary>
        /// <param name="key"></param>
        public static void Destroy(TKeyType key)
        {
            if (!InstanceMap.ContainsKey(key)) { return; }
            SoloQueueWorker<TKeyType> instance;
            InstanceMap.TryRemove(key, out instance);
            instance.TurnOff();
        }

        protected SoloQueueWorker(TKeyType key, IConnectionProvider factory, IWorkerStatisticsProvider statisticsProvider) : base(factory, statisticsProvider)
        {
            if (InstanceMap.ContainsKey(key))
            {
                throw new InvalidOperationException($"A worker with ID {InstanceMap[key].Id} currently exists for key {key}. " +
                                                    "Destroy it by calling the static Destroy method before trying to create another worker for this key.");
            }
            Key = key;
            InstanceMap[key] = this;
        }
    }
}
