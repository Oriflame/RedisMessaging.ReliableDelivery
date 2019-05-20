namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe.Validation
{
    /// <inheritdoc />
    public class MessageValidator : IMessageValidator
    {
        /// <inheritdoc />
        public long LastMessageId { get; private set; }

        /// <inheritdoc />
        public virtual IMessageValidationResult Validate(Message message)
        {
            var previousMessageId = LastMessageId;
            if (previousMessageId < message.Id)
            {
                LastMessageId = message.Id;
            }

            // if this validator was not synchronized with LastProcessedMessageId from Redis value
            // then we cannot validate a message yet
            if (previousMessageId == default)
            {
                return SuccessValidationResult.Instance;
            }

            if (IsAlreadyProcessed(previousMessageId, message.Id))
            {
                return AlreadyProcessedValidationResult.Instance;
            }

            if (IsMessageMissing(previousMessageId, message.Id))
            {
                return new ValidationResultForMissingMessages(previousMessageId);
            }

            return SuccessValidationResult.Instance;

        }

        /// <summary>
        /// A detection method checking whether a message was already received (and processed)
        /// by this validator. If yes then a current message is being received again.
        /// </summary>
        /// <param name="previousMessageId">message ID of a message that was fully processed, <see cref="LastMessageId"/></param>
        /// <param name="currentMessageId">message ID of a message that was just received</param>
        /// <returns>true if a message was already received and processed</returns>
        protected virtual bool IsAlreadyProcessed(long previousMessageId, long currentMessageId)
        {
            return previousMessageId >= currentMessageId;
        }

        /// <summary>
        /// A detection checking whether there exists a gap in message IDs between
        /// previousMessageId and currentMessageId.
        /// </summary>
        /// <param name="previousMessageId">message ID of a message that was fully processed, <see cref="LastMessageId"/></param>
        /// <param name="currentMessageId">message ID of a message that was just received</param>
        /// <returns>true if there exists a gap in message IDs between previousMessageId and currentMessageId</returns>
        protected virtual bool IsMessageMissing(long previousMessageId, long currentMessageId)
        {
            return previousMessageId + 1 < currentMessageId;
        }
    }
}