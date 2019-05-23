using System;

namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe
{
    /// <summary>
    /// Provides feature to check that subscription channel integrity,
    /// i.e. that channel is receiving messages as expected.
    /// </summary>
    public interface IMessageDeliveryChecker
    {
        /// <summary>
        /// Check whether there are some messages in Redis server pending,
        /// i.e. they were not received and handled by <see cref="IMessageHandler"/>
        /// </summary>
        void CheckForMissedMessages();

        /// <summary>
        /// Date at which last message was processed either by message deliver to a subscribed channel
        /// or <see cref="CheckForMissedMessages"/>
        /// </summary>
        DateTime LastActivityAt { get; }
    }
}