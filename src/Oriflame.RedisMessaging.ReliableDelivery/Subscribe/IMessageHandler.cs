using System;

namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe
{
    /// <summary>
    /// Represents logic for processing received messages, both expected and unexpected (e.g. out of order)
    /// </summary>
    internal interface IMessageHandler
    {
        /// <summary>
        /// function called when an expected message is received, e.g. its sequence number is not out of order
        /// </summary>
        void OnExpectedMessage(string channel, Message message);

        void OnMissedMessage(string channel, Message message);
        
        void OnDuplicatedMessage(string channel, Message message);

        void OnMissingMessages(string channel, long missingMessagesCount);
    }
}