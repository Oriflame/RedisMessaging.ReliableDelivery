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

        public void OnExpectedMessage(string physicalOrLogicalChannel, Message message)
        {
            _onExpectedMessage(physicalOrLogicalChannel, message);
        }

        public void OnMissedMessage(string physicalOrLogicalChannel, Message message)
        {
            _onMissedMessage(physicalOrLogicalChannel, message);
        }

        public void OnDuplicatedMessage(string physicalOrLogicalChannel, Message message)
        {
            _onDuplicatedMessage(physicalOrLogicalChannel, message);
        }

        public void OnMissingMessages(string physicalOrLogicalChannel, long missingMessagesCount)
        {
            _onMissingMessages(physicalOrLogicalChannel, missingMessagesCount);
        }
    }
}