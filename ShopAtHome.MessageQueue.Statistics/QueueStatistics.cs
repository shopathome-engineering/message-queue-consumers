using System.Collections.Generic;
using System.Linq;

namespace ShopAtHome.MessageQueue.Statistics
{
    public class QueueStatistics
    {
        public QueueStatistics()
        {
            MessageResolutionTimes = new List<float>();
        }

        /// <summary>
        /// The amount of time (in seconds) it takes for a message to be read from the queue, processed, and a completed receipt returned to the bus
        /// </summary>
        public float MeanMessageResolutionTime => MessageResolutionTimes.Any() ? MessageResolutionTimes.Sum()/MessageResolutionTimes.Count() : 1; // Dividing by zero is frowned upon

        public IEnumerable<float> MessageResolutionTimes { get; set; }
    }
}
