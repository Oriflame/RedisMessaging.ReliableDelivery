using System.Threading;

namespace RedisMessaging.ReliableDelivery.Subscribe
{
    public class MessageValidator : IMessageValidator
    {
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
            if (previousMessageId == default(long))
            {
                return MessageValidationResult.Success;
            }

            if (IsAlreadyProcessed(previousMessageId, message.Id))
            {
                return MessageValidationResult.MessageAgain;
            }

            if (IsMessageMissing(previousMessageId, message.Id))
            {
                return new ValidationResultForMissingMessages(previousMessageId);
            }

            return MessageValidationResult.Success;

        }

        protected virtual bool IsAlreadyProcessed(long previousMessageId, long currentMessageId)
        {
            return previousMessageId >= currentMessageId;
        }

        protected virtual bool IsMessageMissing(long previousMessageId, long currentMessageId)
        {
            return previousMessageId + 1 < currentMessageId;
        }
    }
}