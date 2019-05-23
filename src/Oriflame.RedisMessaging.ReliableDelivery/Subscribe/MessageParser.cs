using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Oriflame.RedisMessaging.ReliableDelivery.Publish;

namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe
{
    /// <summary>
    /// Parses a message so that successful parsing wil result in a structured message containing
    /// message ID and message content.
    /// </summary>
    internal class MessageParser : IMessageParser
    {
        private static readonly char[] Separator = { ReliablePublisher.MessagePartSeparator };
        private readonly ILogger<MessageParser> _log;

        /// <summary>
        /// Creates a parser responsible for analyzing and parsing a waw message
        /// </summary>
        /// <param name="log">an optional logger for tracing internal activity of this parser</param>
        public MessageParser(ILogger<MessageParser> log = null)
        {
            _log = log ?? NullLogger<MessageParser>.Instance;
        }

        /// <inheritdoc />
        public bool TryParse(string message, out Message parsedMessage)
        {
            var messageParts = message.Split(Separator, 2);
            if (messageParts.Length != 2)
            {
                _log.LogWarning($"Message format should be 'messageId{ReliablePublisher.MessagePartSeparator}messageContent'. It contains {{MessagePartsCount}} parts.", messageParts.Length);

                parsedMessage = Message.Undefined;
                return false;
            }

            if (!long.TryParse(messageParts[0], out var messageId))
            {
                _log.LogWarning("MessageId should be convertible to integer (messageId length={MessageIdLength}).", messageParts[0].Length);
                parsedMessage = Message.Undefined;
                return false;
            }

            var messageContent = messageParts[1];
            parsedMessage = new Message(messageId, messageContent);

            return true;
        }
    }
}