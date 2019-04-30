using Microsoft.Extensions.Logging;

namespace RedisMessaging.ReliableDelivery.Subscribe
{
    public class ValidationFailureLogger : IMessageValidationFailureHandler
    {
        private readonly ILogger<ValidationFailureLogger> _logger;

        public ValidationFailureLogger(ILogger<ValidationFailureLogger> logger)
        {
            _logger = logger;
        }

        public void OnInvalidMessage(string channel, string message, long messageId, long previousMessageId)
        {
            _logger.LogError("Invalid message in channel '{channel}'. messageId={messageId}, previousMessageId={previousMessageId}",
                channel, messageId, previousMessageId);
        }

        public void OnInvalidMessageFormat(string channel, string rawMessage)
        {
            _logger.LogError("Invalid message format in channel '{channel}'. rawMessage={rawMessage}",
                channel, rawMessage);
        }
    }
}