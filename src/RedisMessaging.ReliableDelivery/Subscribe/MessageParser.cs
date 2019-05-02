using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RedisMessaging.ReliableDelivery.Publish;

namespace RedisMessaging.ReliableDelivery.Subscribe
{
    public class MessageParser : IMessageParser
    {
        private static readonly char[] Separator = { ReliablePublisher.MessagePartSeparator };
        private readonly ILogger<MessageParser> _log;

        public MessageParser(ILogger<MessageParser> log = null)
        {
            _log = log ?? NullLogger<MessageParser>.Instance;
        }

        // TODO
        //public void OnInvalidMessage(string channel, string message, long messageId, long previousMessageId)
        //{
        //    _logger.LogError("Invalid message in channel '{channel}'. messageId={messageId}, previousMessageId={previousMessageId}",
        //        channel, messageId, previousMessageId);
        //}

        /// <inheritdoc />
        public bool TryParse(string message, out Message parsedMessage)
        {
            var messageParts = message.Split(Separator, 2);
            if (messageParts.Length != 2)
            {
                LogWarning($"Message format should be 'messageId{ReliablePublisher.MessagePartSeparator}messageContent'. It contains {{MessagePartsCount}} parts.", messageParts.Length);

                parsedMessage = Message.Undefined;
                return false;
            }

            if (!long.TryParse(messageParts[0], out var messageId))
            {
                LogWarning("MessageId should be convertible to integer (messageId length={MessageIdLength}).", messageParts[0].Length);
                parsedMessage = Message.Undefined;
                return false;
            }

            var messageContent = messageParts[1];
            parsedMessage = new Message(messageId, messageContent);

            return true;
        }

        private void LogWarning(string messageTemplate, params object[] parameters)
        {
            _log?.LogWarning(messageTemplate, parameters);
        }
    }
}