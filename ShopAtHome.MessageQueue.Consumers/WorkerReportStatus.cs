namespace ShopAtHome.MessageQueue.Consumers
{
    /// <summary>
    /// Enumeration of possible worker states
    /// </summary>
    public enum WorkerReportStatus
    {
        /// <summary>
        /// All work has been completed successfully
        /// </summary>
        TaskComplete,
        /// <summary>
        /// There is no work to be done
        /// </summary>
        NoWork,
        /// <summary>
        /// The worker entered an unrecoverable state and crashed
        /// </summary>
        FatalError,
        /// <summary>
        /// Too many workers have crashed trying to process a message, and it needs to be removed from the work queue and reviewed by a developer
        /// </summary>
        ExileMessage
    }
}
