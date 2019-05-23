using System.Threading.Tasks;
using Oriflame.RedisMessaging.ReliableDelivery.Publish;

namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe
{
    /// <summary>
    /// API for publishing messages and receiving them reliably
    /// </summary>
    public interface IReliablePublisherSubscriber : IReliablePublisher
    {
        /// <summary>
        /// <see cref="IReliableSubscriber.Subscribe"/>
        /// </summary>
        /// <param name="channel">name of a channel from which a message should be received</param>
        /// <param name="onExpectedMessage">Action to be called when a message received was expected</param>
        /// <param name="onMissedMessage">Action to be called when a message out of order is received</param>
        /// <param name="onDuplicatedMessage">Action to be called when a message was received recently and now it is received again</param>
        /// <param name="onMissingMessages">Action to be called when previously missed messages could not be retrieved from Redis</param>
        /// <returns>checker for channel integrity validation</returns>
        IMessageDeliveryChecker Subscribe(
            string channel,
            MessageAction onExpectedMessage,
            MessageAction onMissedMessage = default,
            MessageAction onDuplicatedMessage = default,
            MessagesCountAction onMissingMessages = default);

        /// <summary>
        /// <see cref="IReliableSubscriber.SubscribeAsync"/>
        /// </summary>
        /// <param name="channel">name of a channel from which a message should be received</param>
        /// <param name="onExpectedMessage">Action to be called when a message received was expected</param>
        /// <param name="onMissedMessage">Action to be called when a message out of order is received</param>
        /// <param name="onDuplicatedMessage">Action to be called when a message was received recently and now it is received again</param>
        /// <param name="onMissingMessages">Action to be called when previously missed messages could not be retrieved from Redis</param>
        /// <returns>checker for channel integrity validation</returns>
        Task<IMessageDeliveryChecker> SubscribeAsync(
            string channel,
            MessageAction onExpectedMessage,
            MessageAction onMissedMessage = default,
            MessageAction onDuplicatedMessage = default,
            MessagesCountAction onMissingMessages = default);

        /// <summary>
        /// <see cref="IReliableSubscriber.Unsubscribe"/>
        /// </summary>
        /// <param name="channel">A channel to be unregistered from receiving messages</param>
        void Unsubscribe(string channel);

        /// <summary>
        /// <see cref="IReliableSubscriber.UnsubscribeAsync"/>
        /// </summary>
        /// <param name="channel">A channel to be unregistered from receiving messages</param>
        /// <returns>A continuation task</returns>
        Task UnsubscribeAsync(string channel);
    }
}