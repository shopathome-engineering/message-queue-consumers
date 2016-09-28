using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;

namespace ShopAtHome.MessageQueue.Statistics
{
    public class CacheStatisticsProvider : IProcessorStatisticsProvider, IWorkerStatisticsProvider
    {
        private const string QueueStatisticsCacheKey = "TPC_QS_{0}";
        private const string WorkerStatisticsCacheKey = "TPC_WS_{0}";
        private const int MaxNumTimesToRemember = 1000; // 4kb seems like enough to remember
        private static readonly MemoryCache Cache = MemoryCache.Default;

        public QueueStatistics GetQueueStatistics(string queueName)
        {
            var statistics = Cache.Get(string.Format(QueueStatisticsCacheKey, queueName)) as QueueStatistics;
            return statistics ?? new QueueStatistics();
        }

        public void RecordQueueMessageCompletionTime(string queueName, TimeSpan elapsedTime)
        {
            var existingStats = GetQueueStatistics(queueName);
            var messageResolutionTimes = existingStats.MessageResolutionTimes.ToList();
            messageResolutionTimes.Add((float)(elapsedTime.TotalMilliseconds * 1e-3));

            if (messageResolutionTimes.Count > MaxNumTimesToRemember)
            {
                messageResolutionTimes = ClearOutliers(messageResolutionTimes);
            }

            existingStats.MessageResolutionTimes = messageResolutionTimes;
            SetStatsInCache(queueName, existingStats);
        }

        private List<float> ClearOutliers(List<float> messageClearTimes)
        {
            var result = messageClearTimes;
            result.Sort();
            result.Remove(result.First());
            result.Remove(result.Last());
            return result;
        }

        private static void SetStatsInCache(string queueName, QueueStatistics stats)
        {
            Cache.Set(string.Format(QueueStatisticsCacheKey, queueName), stats, new CacheItemPolicy { SlidingExpiration = TimeSpan.FromHours(8) });
        }

        public bool WorkerFailuresExceedThreshold(Type workerType, Exception exception, Message<object> message)
        {
            var key = MakeWorkerExceptionCacheKey(workerType, exception, message);
            var recentFailures = Cache.Get(key) as MessageStatistics;
            // Dumb static threshold for now - needs re-working when we get to building a real statistics provider
            return recentFailures?.NumTimesFailed > 10;
        }

        public void RecordWorkerFailure(Type workerType, Exception exception, Message<object> message)
        {
            var key = MakeWorkerExceptionCacheKey(workerType, exception, message);
            var existingFailures = Cache.Get(key) as MessageStatistics ?? new MessageStatistics();
            existingFailures.NumTimesFailed++;
            Cache.Set(key, existingFailures, new CacheItemPolicy { SlidingExpiration = TimeSpan.FromHours(8)});
        }

        private static string MakeWorkerExceptionCacheKey(Type workerType, Exception error, Message<object> message)
        {
            return string.Format(WorkerStatisticsCacheKey, workerType + error.ToString() + string.Join(string.Empty, message.Data.SelectMany(MessageStatistics.MakeFingerprint)));
        }
    }
}
