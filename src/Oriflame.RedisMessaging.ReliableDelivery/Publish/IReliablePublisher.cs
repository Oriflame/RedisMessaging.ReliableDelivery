using System;
using System.Threading.Tasks;

namespace Oriflame.RedisMessaging.ReliableDelivery.Publish
{
    /// <summary>
    /// Represents an object responsible for publishing messages to a given channel
    /// </summary>
    public interface IReliablePublisher
    {
        /// <summary>
        /// Publishes a message synchronously
        /// </summary>
        /// <param name="channel">name of a channel into that a message should be published</param>
        /// <param name="message">a raw message to be published</param>
        /// <param name="messageExpiration">Time-to live (TTL) of a message stored in Redis server. Default is 10 minutes.</param>
        void Publish(string channel, string message, TimeSpan messageExpiration = default);

        /// <summary>
        /// Publishes a message asynchronously
        /// </summary>
        /// <param name="channel">name of a channel into that a message should be published</param>
        /// <param name="message">a raw message to be published</param>
        /// <param name="messageExpiration">Time-to live (TTL) of a message stored in Redis server. Default is 10 minutes.</param>
        /// <returns>A continuation task</returns>
        Task PublishAsync(string channel, string message, TimeSpan messageExpiration = default);
    }
}