using System;
using System.Collections.Generic;
using System.Linq;
using ShopAtHome.MessageQueue.Consumers.Configuration;
using ShopAtHome.MessageQueue.Consumers.Messages;
using ShopAtHome.MessageQueue.Exceptions;
using ShopAtHome.MessageQueue.Statistics;

namespace ShopAtHome.MessageQueue.Consumers
{
    /// <summary>
    /// Basic functionality around a consumer that acts on message data
    /// </summary>
    public abstract class BaseWorker : BaseActor
    {
        /// <summary>
        /// Provides access to worker execution statistics
        /// </summary>
        protected readonly IWorkerStatisticsProvider StatisticsProvider;

        /// <summary>
        /// If set, the identifier of the queue to which data is written after this worker has finished working on it
        /// </summary>
        protected string NextQueue;

        /// <summary>
        /// Builds the base worker with the required dependencies
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="statisticsProvider"></param>
        protected BaseWorker(IConnectionProvider factory, IWorkerStatisticsProvider statisticsProvider) : base(factory)
        {
            StatisticsProvider = statisticsProvider;
        }

        /// <summary>
        /// Configures this worker
        /// </summary>
        /// <param name="configuration"></param>
        public virtual void ConfigureWorker(IWorkerConfiguration configuration)
        {
            try
            {
                NextQueue = configuration.NextQueue;
            }
            catch (Exception ex)
            {
                ErrorAction(ex);
            }
        }

        /// <summary>
        /// Sends a report that this worker has nothing to do
        /// </summary>
        protected void SendNoWorkReport()
        {
            try
            {
                var message = Message<WorkerReport>.WithData(new WorkerReport
                {
                    SourceQueue = SourceQueue,
                    WorkerId = Id,
                    Status = WorkerReportStatus.NoWork
                });
                ExecuteOnQueue<WorkerReport>(ReportQueue, q => q.Write(message));
            }
            catch (Exception ex)
            {
                SendErrorReport(ex);
                ErrorAction(ex);
            }
        }

        /// <summary>
        /// Reads a message from the SourceQueue, executes the provided workMethod on each datum in the message, confirms the receipt, then reads the next message from the source queue
        /// until the queue has been emptied
        /// </summary>
        /// <typeparam name="TMessageDataType"></typeparam>
        /// <param name="workMethod"></param>
        protected void WorkQueueUntilEmpty<TMessageDataType>(Action<TMessageDataType> workMethod)
        {
            try
            {
                Running = true;
                using (var sourceQueueConnection = Factory.ConnectToQueue<TMessageDataType>(SourceQueue))
                {
                    Message<TMessageDataType> message = null;
                    try
                    {
                        message = GetNextMessage(sourceQueueConnection);
                        while (message != null)
                        {
                            WorkBatch(message.Data, workMethod);
                            sourceQueueConnection.ConfirmMessageReceipt(message);
                            message = sourceQueueConnection.ReadNext();
                        }
                    }
                    catch (Exception ex)
                    {
                        HandleWorkFailure(message, ex, sourceQueueConnection);
                        throw;
                    }
                }
                SendNoWorkReport();
            }
            catch (Exception ex)
            {
                TurnOff();
                SendErrorReport(ex);
                throw;
            }
        }

        protected Message<TMessageDataType> GetNextMessage<TMessageDataType>(IQueueConnection<TMessageDataType> queueConnection)
        {
            try
            {
                return queueConnection.ReadNext();
            }
            catch (MessageSerializationFailureException msfe)
            {
                HandleSerializationFailure(Message<MessageSerializationFailureException>.WithData(msfe));
                return null;
            }
        }

        protected void HandleWorkFailure<TMessageDataType>(Message<TMessageDataType> message, Exception ex, IQueueConnection<TMessageDataType> sourceQueueConnection)
        {
            if (message == null)
            {
                // Serialization failure! The MQ library will kill this for us
                return;
            }
            var downcastMessage = Message<object>.WithData(message.Data.Select(x => x as object));
            StatisticsProvider.RecordWorkerFailure(GetType(), ex, downcastMessage);
            // Check to see if siblings have recent failures, and how many, and if they are of the same type, and from this message in particular
            if (StatisticsProvider.WorkerFailuresExceedThreshold(GetType(), ex, downcastMessage))
            {
                // This message is crap - ack and exile it
                sourceQueueConnection.ConfirmMessageReceipt(message);
                ExecuteOnQueue<WorkerReport>(ReportQueue, q => q.Write(Message<WorkerReport>.WithData(WorkerReport.CreateExileMessageRequest(SourceQueue, ex, downcastMessage, Id))));
            }
        }

        /// <summary>
        /// Reads a message from the SourceQueue, executes the provided function on the message and writes the resulting data to the NextQueue
        /// Repeats until the SourceQueue is empty
        /// </summary>
        /// <typeparam name="TMessageDataType"></typeparam>
        /// <typeparam name="TNextQueueMessageDataType"></typeparam>
        /// <param name="compositionFunction"></param>
        protected void WorkQueueUntilEmpty<TMessageDataType, TNextQueueMessageDataType>(Func<TMessageDataType, TNextQueueMessageDataType> compositionFunction)
        {
            try
            {
                Running = true;
                using (var sourceQueueConnection = Factory.ConnectToQueue<TMessageDataType>(SourceQueue))
                {
                    Message<TMessageDataType> message = null;
                    try
                    {
                        message = sourceQueueConnection.ReadNext();
                        while (message != null)
                        {
                            var nextQueueMessageData = WorkBatch(message.Data, compositionFunction);
                            ExecuteOnQueue<TNextQueueMessageDataType>(NextQueue, q => q.Write(Message<TNextQueueMessageDataType>.WithData(nextQueueMessageData)));
                            sourceQueueConnection.ConfirmMessageReceipt(message);
                            message = sourceQueueConnection.ReadNext();
                        }
                    }
                    catch (MessageSerializationFailureException msfe)
                    {
                        HandleSerializationFailure(Message<MessageSerializationFailureException>.WithData(msfe));
                    }
                    catch (Exception ex)
                    {
                        HandleWorkFailure(message, ex, sourceQueueConnection);
                        throw;
                    }
                }
                SendNoWorkReport();
            }
            catch (Exception ex)
            {
                TurnOff();
                SendErrorReport(ex);
                throw;
            }
        }

        protected void HandleSerializationFailure(Message<MessageSerializationFailureException> message)
        {
            var downcastMessage = Message<object>.WithData(message.Data.Select(x => x as object));
            ExecuteOnQueue<WorkerReport>(ReportQueue, q => q.Write(Message<WorkerReport>.WithData(WorkerReport.CreateExileMessageRequest(SourceQueue, message.Data.FirstOrDefault(), downcastMessage, Id))));
        }

        /// <summary>
        /// Sends a report that this worker has finished all of its available work, and has stopped.
        /// </summary>
        /// <param name="timeElapsed"></param>
        protected void SendWorkCompleteReport(TimeSpan timeElapsed)
        {
            try
            {
                var report = Message<WorkerReport>.WithData(new WorkerReport
                {
                    SourceQueue = SourceQueue,
                    WorkerId = Id,
                    ElapsedTime = timeElapsed,
                    Status = WorkerReportStatus.TaskComplete
                });
                ExecuteOnQueue<WorkerReport>(ReportQueue, q => q.Write(report));
            }
            catch (Exception ex)
            {
                SendErrorReport(ex);
                ErrorAction(ex);
            }
        }

        /// <summary>
        /// Executes the provided function on each item in the batch, yielding the result of the invocation. Sends a Work Complete message to the report queue when finished
        /// </summary>
        /// <typeparam name="TMessageDataType"></typeparam>
        /// <typeparam name="TNextQueueMessageDataType"></typeparam>
        /// <param name="data"></param>
        /// <param name="compositionFunction"></param>
        /// <returns></returns>
        protected IEnumerable<TNextQueueMessageDataType> WorkBatch<TMessageDataType, TNextQueueMessageDataType>(List<TMessageDataType> data, Func<TMessageDataType, TNextQueueMessageDataType> compositionFunction)
        {
            try
            {
                var start = DateTime.Now;
                var results = data.Select(compositionFunction).ToList();
                var elapsed = DateTime.Now - start;
                SendWorkCompleteReport(elapsed);
                return results;
            }
            catch (Exception ex)
            {
                SendErrorReport(ex);
                ErrorAction(ex);
                throw;
            }
        }

        /// <summary>
        /// Executes the provided action on each item in the batch, and sends a Work Complete message to the report queue when finished
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="batch"></param>
        /// <param name="workFunction"></param>
        protected void WorkBatch<T>(IEnumerable<T> batch, Action<T> workFunction)
        {
            try
            {
                var start = DateTime.Now;
                foreach (var datum in batch)
                {
                    workFunction(datum);
                }
                var elapsed = DateTime.Now - start;
                SendWorkCompleteReport(elapsed);
            }
            catch (Exception ex)
            {
                SendErrorReport(ex);
                ErrorAction(ex);
                throw;
            }
        }
    }
}
