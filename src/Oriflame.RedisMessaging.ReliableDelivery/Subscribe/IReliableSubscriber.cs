using System.Threading.Tasks;

namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe
{
    /// <summary>
    /// Represents an object responsible for receiving messages from a given channel.
    /// When a message is received then a subscriber should validate whether
    /// a message order is valid, i.e. whether some messages were accidentally lost
    /// </summary>
    public interface IReliableSubscriber
    {
        /// <summary>
        /// Subscribes (synchronously) a <see cref="IMessageHandler"/>
        /// that is expected to process messages received.
        /// </summary>
        /// <param name="handler">an object responsible for processing messages received</param>
        void Subscribe(IMessageHandler handler);

        /// <summary>
        /// Subscribes (asynchronously) a <see cref="IMessageHandler"/>
        /// that is expected to process messages received.
        /// </summary>
        /// <param name="handler">an object responsible for processing messages received</param>
        /// <returns>A continuation task</returns>
        Task SubscribeAsync(IMessageHandler handler);

        /// <summary>
        /// Removes (synchronously) a handlers previously subscribed via this subscriber
        /// to a given channel.
        /// </summary>
        /// <param name="channel">name of a channel from which a message should be received</param>
        void Unsubscribe(string channel);

        /// <summary>
        /// Removes (asynchronously) a handlers previously subscribed via this subscriber
        /// to a given channel.
        /// </summary>
        /// <param name="channel">name of a channel from which a message should be received</param>
        /// <returns>A continuation task</returns>
        Task UnsubscribeAsync(string channel);

        /// <summary>
        /// Removes (synchronously) all handlers previously subscribed via this subscriber.
        /// </summary>
        void UnsubscribeAll();

        /// <summary>
        /// Removes (asynchronously) all handlers previously subscribed via this subscriber.
        /// </summary>
        /// <returns>A continuation task</returns>
        Task UnsubscribeAllAsync();
    }
}