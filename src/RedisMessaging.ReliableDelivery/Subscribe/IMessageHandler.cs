using StackExchange.Redis;

namespace RedisMessaging.ReliableDelivery.Subscribe
{
    public interface IMessageHandler
    {
        RedisChannel Channel { get; }

        void HandleMessage(Message message);
        void HandleNewestMessages();
    }
}