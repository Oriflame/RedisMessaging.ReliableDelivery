namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe
{
    public class MessageValidator : IMessageValidator
    {
        // TODO is locking needed?
        private readonly object _lock = new object();

        internal long LastMessageId { get; private set; }

        /// <inheritdoc />
        public virtual IMessageValidationResult Validate(Message message)
        {
            long previousMessageId;
            lock (_lock)
            {
                previousMessageId = LastMessageId;
                if (previousMessageId < message.Id)
                {
                    LastMessageId = message.Id;
                }
            }

            // if this validator was not synchronized with LastProcessedMessageId from Redis value
            // then we cannot validate a message yet
            if (previousMessageId == default(long))
            {
                return MessageValidationResult.Success;
            }

            var isMessageValid = IsMessageValid(previousMessageId, message.Id);
            if (isMessageValid)
            {
                return MessageValidationResult.Success;
            }

            return new ValidationResultForMissingMessages(previousMessageId);
        }

        protected virtual bool IsMessageValid(long previousMessageId, long currentMessageId)
        {
            return previousMessageId + 1 == currentMessageId;
        }
    }
}