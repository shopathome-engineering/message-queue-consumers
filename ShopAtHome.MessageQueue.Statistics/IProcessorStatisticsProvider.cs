using System;

namespace ShopAtHome.MessageQueue.Statistics
{
    public interface IProcessorStatisticsProvider
    {
        QueueStatistics GetQueueStatistics(string queueName);

        void RecordQueueMessageCompletionTime(string queueName, TimeSpan elapsedTime);
    }
}
