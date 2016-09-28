using System;

namespace ShopAtHome.MessageQueue.Statistics
{
    public interface IWorkerStatisticsProvider
    {
        bool WorkerFailuresExceedThreshold(Type workerType, Exception exception, Message<object> message);

        void RecordWorkerFailure(Type workerType, Exception exception, Message<object> message);
    }
}
