using System.Collections.Generic;

namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe
{
    /// <summary>
    /// Provides capability to get published messages from Redis server 
    /// </summary>
    public interface IMessageLoader
    {
        /// <summary>
        /// Loads messages from Redis server that were already published
        /// </summary>
        /// <param name="channelName">name of a channel to that a message was published</param>
        /// <param name="fromMessageId">min message id for which we need to get messages</param>
        /// <param name="toMessageId">max message id for which we need to get messages</param>
        /// <returns></returns>
        IEnumerable<Message> GetMessages(string channelName, long fromMessageId, long toMessageId = long.MaxValue);
    }
}