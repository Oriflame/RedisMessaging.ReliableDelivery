﻿namespace RedisMessaging.ReliableDelivery.Subscribe
{
    /// <summary>
    /// Validator providing verification if a message received.
    /// </summary>
    public interface IMessageValidator
    {
        /// <returns>true when a consequent execution is allowed, false when the message should not be processed</returns>
        bool Validate(string channel, string message, long messageId);
    }
}