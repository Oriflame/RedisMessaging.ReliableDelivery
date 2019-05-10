using System;
using StackExchange.Redis;

namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe
{
    /// <summary>
    /// Represents logic for processing received messages, both expected and unexpected (e.g. out of order)
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// name of a channel from that a message is received and processed
        /// </summary>
        string Channel { get; }

        /// <summary>
        /// Processes a received message
        /// </summary>
        /// <param name="message">received message that should be processed</param>
        void HandleMessage(Message message);
        
        /// <summary>
        /// Check whether there are some messages in Redis server pending,
        /// i.e. they were not received and handled by this <see cref="IMessageHandler"/>
        /// </summary>
        void CheckMissedMessages();

        /// <summary>
        /// Date at which last message was processed either vua <see cref="HandleMessage"/>
        /// or <see cref="CheckMissedMessages"/>
        /// </summary>
        DateTime LastActivityAt { get; }
    }
}