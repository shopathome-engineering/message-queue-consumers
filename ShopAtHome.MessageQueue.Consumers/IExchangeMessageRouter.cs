namespace ShopAtHome.MessageQueue.Consumers
{
    /// <summary>
    /// An interface that provides routing functionality for messages sent to exchanges
    /// </summary>
    /// <typeparam name="TMessageData"></typeparam>
    public interface IExchangeMessageRouter<in TMessageData>
    {
        /// <summary>
        /// Returns the routing key for the data
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        string MakeRouteKey(TMessageData data);
    }
}
