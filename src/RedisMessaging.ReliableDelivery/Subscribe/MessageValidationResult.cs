namespace RedisMessaging.ReliableDelivery.Subscribe
{
    public class MessageValidationResult : IMessageValidationResult
    {
        public static MessageValidationResult Success { get; } = new MessageValidationResult();
    }
}