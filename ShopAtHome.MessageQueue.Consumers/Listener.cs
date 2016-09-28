using System;
using System.Threading;
using ShopAtHome.MessageQueue.Consumers.Configuration;
using ShopAtHome.MessageQueue.Consumers.Messages;

namespace ShopAtHome.MessageQueue.Consumers
{
    /// <summary>
    /// A poller/monitor/listener
    /// Attaches itself to a queue and polls it for messages - writes a report whenever its configured thresholds are met
    /// </summary>
    public abstract class Listener : BaseActor
    {
        private int _queueThresholdMesssageCount;
        private int _queueThresholdExecutionInterval;
        private int _queuePollInterval;

        /// <summary>
        /// Constructs the listener with the required dependencies
        /// </summary>
        /// <param name="factory"></param>
        protected Listener(IConnectionProvider factory) : base(factory)
        {
        }

        /// <summary>
        /// The identifier of the queue that is being monitored by this listener
        /// </summary>
        public string QueueBeingMonitored => SourceQueue;

        /// <summary>
        /// Configures this listener
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public Listener ConfigureListener(IListenerConfiguration configuration)
        {
            _queuePollInterval = configuration.PollingInterval;
            _queueThresholdExecutionInterval = configuration.QueueReadInterval;
            _queueThresholdMesssageCount = configuration.QueueReadMessageCountThreshold;
            return this;
        }

        /// <summary>
        /// Polls the configured queue and will act if criteria is met based on the poll results
        /// </summary>
        public override void Work()
        {
            try
            {
                Running = true;
                var timeSinceLastReport = new TimeSpan();
                while (true)
                {
                    if (!Running)
                    {
                        break;
                    }
                    var thisLoopStart = DateTime.Now;
                    int messageCount;
                    // listeners don't care about what's inside the messages, so type the listening queue to object so that we can safely de-serialize any kind of data 
                    using (var listenConn = Factory.ConnectToQueue<object>(SourceQueue))
                    {
                        messageCount = listenConn.GetQueueInfo().MessageCount;
                    }
                    if (CreateWorkerCriteraIsMet(messageCount, timeSinceLastReport))
                    {
                        ActOnCreateWorkerCriteriaBeingMet(messageCount);
                        timeSinceLastReport = new TimeSpan();
                    }
                    Wait(_queuePollInterval);
                    timeSinceLastReport = timeSinceLastReport.Add(DateTime.Now - thisLoopStart);
                }
            }
            catch (Exception ex)
            {
                TurnOff();
                ErrorAction(ex);
            }
        }

        /// <summary>
        /// This method should contain the code to be executed when CreateWorkerCriteriaIsMet returns true
        /// </summary>
        /// <param name="messageCount"></param>
        protected virtual void ActOnCreateWorkerCriteriaBeingMet(int messageCount)
        {
            var message = Message<ListenerReport>.WithData(new ListenerReport
            {
                MessageCount = messageCount,
                Queue = SourceQueue
            });
            ExecuteOnQueue<ListenerReport>(ReportQueue, q => q.Write(message));
        }

        /// <summary>
        /// This method, if overridden, should return true if a worker should be created if based on the provided arguments, false otherwise
        /// </summary>
        /// <param name="messageCount"></param>
        /// <param name="timeSinceLastReport"></param>
        /// <returns></returns>
        protected virtual bool CreateWorkerCriteraIsMet(int messageCount, TimeSpan timeSinceLastReport)
        {
            return messageCount >= _queueThresholdMesssageCount || (messageCount > 0 && timeSinceLastReport.TotalSeconds > _queueThresholdExecutionInterval);
        }

        /// <summary>
        /// Thread.Sleep takes milliseconds, I'd rather abstract the conversion
        /// </summary>
        /// <param name="seconds"></param>
        private static void Wait(int seconds)
        {
            Thread.Sleep(seconds * 1000);
        }
    }
}
