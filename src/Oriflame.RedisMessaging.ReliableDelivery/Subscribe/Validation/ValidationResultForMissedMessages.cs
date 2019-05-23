namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe.Validation
{
    /// <summary>
    /// Represents a validation result for the case when there is a gap between
    /// last processed message ID and a currently being processed message ID.
    /// See <see cref="IMessageValidator.Validate(Message)" />
    /// </summary>
    public readonly struct ValidationResultForMissedMessages : IMessageValidationResult
    {
        /// <summary>
        /// Last successfully processed message id
        /// </summary>
        public long LastProcessedMessageId { get; }

        /// <summary>
        /// Creates a validation result.
        /// </summary>
        /// <param name="lastProcessedMessageId">message ID of a message that was fully processed, <see cref="IMessageValidator.LastMessageId"/></param>
        public ValidationResultForMissedMessages(long lastProcessedMessageId)
        {
            LastProcessedMessageId = lastProcessedMessageId;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"MissingMessages:LastProcessedMessageId={LastProcessedMessageId}";
        }
    }
}