namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe
{
    public class MessageValidationResult : IMessageValidationResult
    {
        private readonly string _name;
        public static MessageValidationResult Success { get; } = new MessageValidationResult("success");

        private MessageValidationResult(string name)
        {
            _name = name;
        }

        public override string ToString()
        {
            return _name;
        }
    }
}