namespace RedisMessaging.ReliableDelivery.Subscribe
{
    public interface IMessageParser
    {
        /// <param name="message">raw message delivered from Redis</param>
        /// <param name="parsedMessage">parsed message with fully initialized properties Id and Content in case return value is true</param>
        /// <returns>true when message format is valid</returns>
        bool TryParse(string message, out Message parsedMessage);
    }
}