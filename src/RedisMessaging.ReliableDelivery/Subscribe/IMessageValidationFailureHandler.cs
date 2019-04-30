namespace RedisMessaging.ReliableDelivery.Subscribe
{
    public interface IMessageValidationFailureHandler
    {
        void OnInvalidMessage(string channel, string message, long messageId, long previousMessageId);

        void OnInvalidMessageFormat(string channel, string rawMessage);
    }
}