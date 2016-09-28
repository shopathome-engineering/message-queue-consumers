using System;

namespace ShopAtHome.MessageQueue.Consumers.Messages
{
    /// <summary>
    /// A report from a worker
    /// </summary>
    public class WorkerReport
    {
        /// <summary>
        /// Returns a worker report that contains the information required to successfully report an unrecoverable error event
        /// </summary>
        /// <param name="sourceQueue"></param>
        /// <param name="workerId"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static WorkerReport ForError(string sourceQueue, Guid workerId, Exception error)
        {
            return new WorkerReport
            {
                SourceQueue = sourceQueue,
                WorkerId = workerId,
                Status = WorkerReportStatus.FatalError,
                Exception = error
            };
        }

        /// <summary>
        /// Sends a worker report that informs the application that the provided message has been purged from the queue, and should be placed into a holding queue for manual review
        /// as it exceeded the threshold for allowed worker deaths during attempted processing
        /// </summary>
        /// <param name="sourceQueue"></param>
        /// <param name="error">The exception that the message has been causing in the worker</param>
        /// <param name="messageToBeExiled">This is typed to object to allow any message data to be exiled with this signature</param>
        /// <param name="workerId"></param>
        /// <returns></returns>
        public static WorkerReport CreateExileMessageRequest(string sourceQueue, Exception error, Message<object> messageToBeExiled, Guid workerId)
        {
            return new WorkerReport
            {
                SourceQueue = sourceQueue,
                Exception = error,
                Status = WorkerReportStatus.ExileMessage,
                Message = messageToBeExiled,
                WorkerId = workerId
            };
        }
        
        /// <summary>
        /// The identifier of the queue from which the worker reads data
        /// </summary>
        public string SourceQueue { get; set; }

        /// <summary>
        /// The unique identifier of the worker
        /// </summary>
        public Guid WorkerId { get; set; }

        /// <summary>
        /// The amount of time the worker took to do something
        /// </summary>
        public TimeSpan ElapsedTime { get; set; }

        /// <summary>
        /// The status of the worker as of the time the report was sent
        /// </summary>
        public WorkerReportStatus Status { get; set; }

        /// <summary>
        /// If set, an exception the worker encountered that is relevant to the report being sent
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// If set, contains a message from a work queue that is relevant to the report being sent
        /// </summary>
        public Message<object> Message { get; set; }
    }
}
