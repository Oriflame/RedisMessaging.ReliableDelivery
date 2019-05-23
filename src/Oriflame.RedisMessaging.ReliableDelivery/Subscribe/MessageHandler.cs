namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe
{
    internal class MessageHandler : IMessageHandler
    {
        private static readonly MessageAction _noop = (channel, message) => { };
        private static readonly MessagesCountAction _noop2 = (channel, messageIdFrom) => { };
        private readonly MessageAction _onExpectedMessage;
        private readonly MessageAction _onMissedMessage;
        private readonly MessageAction _onDuplicatedMessage;
        private readonly MessagesCountAction _onMissingMessages;

        public MessageHandler(
            MessageAction onExpectedMessage,
            MessageAction onMissedMessage = default,
            MessageAction onDuplicatedMessage = default,
            MessagesCountAction onMissingMessages = default)
        {
            _onExpectedMessage = onExpectedMessage;
            _onMissedMessage = onMissedMessage ?? onExpectedMessage;
            _onDuplicatedMessage = onDuplicatedMessage ?? _noop;
            _onMissingMessages = onMissingMessages ?? _noop2;
        }

        public void OnExpectedMessage(string channel, Message message)
        {
            _onExpectedMessage(channel, message);
        }

        public void OnMissedMessage(string channel, Message message)
        {
            _onMissedMessage(channel, message);
        }

        public void OnDuplicatedMessage(string channel, Message message)
        {
            _onDuplicatedMessage(channel, message);
        }

        public void OnMissingMessages(string channel, long missingMessagesCount)
        {
            _onMissingMessages(channel, missingMessagesCount);
        }
    }
}