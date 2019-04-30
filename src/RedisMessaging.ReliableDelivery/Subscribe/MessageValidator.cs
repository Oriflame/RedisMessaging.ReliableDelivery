namespace RedisMessaging.ReliableDelivery.Subscribe
{
    public class MessageValidator : IMessageValidator
    {
        private readonly IMessageValidationFailureHandler _validationFailureHandler;

        private readonly object _lock = new object();
        private readonly bool _shouldHandleInvalidMessage;

        public MessageValidator(IMessageValidationFailureHandler messageValidationFailureHandler, bool shouldHandleInvalidMessage)
        {
            _validationFailureHandler = messageValidationFailureHandler;
            _shouldHandleInvalidMessage = shouldHandleInvalidMessage;
        }

        public long LastMessageId { get; private set; }

        /// <inheritdoc />
        public virtual bool Validate(string channel, string message, long messageId)
        {
            long previousMessageId;
            lock (_lock)
            {
                previousMessageId = LastMessageId;
                if (previousMessageId < messageId)
                {
                    LastMessageId = messageId;
                }
            }

            // if this validator was not synchronized with LastMessageId from Redis value
            // then we cannot validate a message yet
            if (previousMessageId == default(long))
            {
                return true;
            }

            var isMessageValid = IsMessageValid(previousMessageId, messageId);
            if (isMessageValid)
            {
                return true;
            }

            _validationFailureHandler.OnInvalidMessage(channel, message, messageId, previousMessageId);
            return ShouldHandleInvalidMessage(message, messageId);
        }

        protected virtual bool IsMessageValid(long previousMessageId, long currentMessageId)
        {
            return previousMessageId + 1 == currentMessageId;
        }

        protected virtual bool ShouldHandleInvalidMessage(string message, long messageId)
        {
            return _shouldHandleInvalidMessage;
        }
    }
}