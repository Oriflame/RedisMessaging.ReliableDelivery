namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe.Validation
{
    /// <summary>
    /// Validator providing verification if a message received.
    /// </summary>
    public interface IMessageValidator
    {
        /// <returns>true when a consequent execution is allowed, false when the message should not be processed</returns>
        IMessageValidationResult Validate(Message message);

        /// <summary>
        /// Message ID of a message that was fully processed (both successfully or with errors) last time
        /// </summary>
        long LastMessageId { get; }
    }
}