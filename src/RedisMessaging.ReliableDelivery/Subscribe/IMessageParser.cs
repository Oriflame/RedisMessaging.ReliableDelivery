namespace RedisMessaging.ReliableDelivery.Subscribe
{
    public interface IMessageParser
    {
        /// <param name="message">raw message delivered from Redis</param>
        /// <param name="parsedMessage">long - message id, string - message content</param>
        /// <returns>true when message format is valid</returns>
        bool TryParse(string message, out (long, string) parsedMessage);
    }
}