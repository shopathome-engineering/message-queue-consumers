using System;
using ShopAtHome.MessageQueue.Consumers.Configuration;
using ShopAtHome.MessageQueue.Consumers.Messages;

namespace ShopAtHome.MessageQueue.Consumers
{
    /// <summary>
    /// A low-level implementation of the IActor interface that exposes some important protected data
    /// </summary>
    public abstract class BaseActor : IActor
    {
        /// <summary>
        /// The connection provider used to communicate with the message queue
        /// </summary>
        protected IConnectionProvider Factory;

        /// <summary>
        /// The identifier of the queue this actor reads its data from
        /// </summary>
        protected string SourceQueue;

        /// <summary>
        /// The identifier of the queue to which this actor sends reports
        /// </summary>
        protected string ReportQueue;

        /// <summary>
        /// Is this actor currently alive and doing something?
        /// </summary>
        protected bool Running;

        /// <summary>
        /// Is this actor currently alive and doing something?
        /// </summary>
        public bool IsRunning => Running;

        /// <summary>
        /// The unique identifier of this actor
        /// </summary>
        public Guid Id { get; private set; }

        protected abstract Action<Exception> ErrorAction { get; }

        /// <summary>
        /// Builds the actor with the required dependencies
        /// </summary>
        /// <param name="factory"></param>
        protected BaseActor(IConnectionProvider factory)
        {
            Factory = factory;
        }

        /// <summary>
        /// Configures the actor with its required information
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public virtual IActor Configure(IActorConfiguration configuration)
        {
            SourceQueue = configuration.SourceQueue;
            Id = Guid.NewGuid();
            ReportQueue = configuration.ReportQueue;
            return this;
        }

        /// <summary>
        /// Sends a report that this worker encountered an unrecoverable error during its execution
        /// </summary>
        /// <param name="ex"></param>
        protected void SendErrorReport(Exception ex)
        {
            try
            {
                var report = Message<WorkerReport>.WithData(WorkerReport.ForError(SourceQueue, Id, ex));
                ExecuteOnQueue<WorkerReport>(ReportQueue, q => q.Write(report));
            }
            catch (Exception errorReportSendException)
            {
                var ae = new AggregateException(ex, errorReportSendException);
                ErrorAction(ae);
            }
        }

        /// <summary>
        /// Invokes the provided action on a connection to the specified queue
        /// </summary>
        /// <typeparam name="TMessageData"></typeparam>
        /// <param name="queueIdentifier"></param>
        /// <param name="action"></param>
        protected void ExecuteOnQueue<TMessageData>(string queueIdentifier, Action<IQueueConnection<TMessageData>> action)
        {
            using (var connection = Factory.ConnectToQueue<TMessageData>(queueIdentifier))
            {
                action(connection);
            }
        }

        /// <summary>
        /// What this actor does
        /// </summary>
        public abstract void Work();

        /// <summary>
        /// Deactivates/stops this worker
        /// </summary>
        public virtual void TurnOff()
        {
            Running = false;
        }
    }
}
