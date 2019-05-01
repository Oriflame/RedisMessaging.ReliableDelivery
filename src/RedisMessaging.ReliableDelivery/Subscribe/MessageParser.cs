using Microsoft.Extensions.Logging;
using RedisMessaging.ReliableDelivery.Publish;

namespace RedisMessaging.ReliableDelivery.Subscribe
{
    public class MessageParser : IMessageParser
    {
        private static readonly char[] Separator = { ReliablePublisher.MessagePartSeparator };
        private readonly ILogger<MessageParser> _log;

        public MessageParser()
        {
        }

        public MessageParser(ILogger<MessageParser> log)
        {
            _log = log;
        }

        // TODO
        //public void OnInvalidMessage(string channel, string message, long messageId, long previousMessageId)
        //{
        //    _logger.LogError("Invalid message in channel '{channel}'. messageId={messageId}, previousMessageId={previousMessageId}",
        //        channel, messageId, previousMessageId);
        //}

        /// <inheritdoc />
        public bool TryParse(string message, out (long, string) parsedMessage)
        {
            var messageParts = ((string)message).Split(Separator, 2);
            if (messageParts.Length != 2)
            {
                LogWarning($"Message format should be 'messageId{ReliablePublisher.MessagePartSeparator}messageContent'. It contains {{MessagePartsCount}} parts.", messageParts.Length);
                parsedMessage.Item1 = 0;
                parsedMessage.Item2 = null;
                return false;
            }

            parsedMessage.Item2 = messageParts[1];

            if (!long.TryParse(messageParts[0], out var messageId))
            {
                LogWarning("MessageId should be convertible to integer (messageId length={MessageIdLength}).", messageParts[0].Length);
                parsedMessage.Item1 = 0;
                return false;
            }

            parsedMessage.Item1 = messageId;

            return true;
        }

        private void LogWarning(string messageTemplate, params object[] parameters)
        {
            _log?.LogWarning(messageTemplate, parameters);
        }
    }
}