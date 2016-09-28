using Newtonsoft.Json;

namespace ShopAtHome.MessageQueue.Statistics
{
    public class MessageStatistics
    {
        public static string MakeFingerprint(object message)
        {
            return JsonConvert.SerializeObject(message);
        }

        public int NumTimesFailed { get; set; }
    }
}
