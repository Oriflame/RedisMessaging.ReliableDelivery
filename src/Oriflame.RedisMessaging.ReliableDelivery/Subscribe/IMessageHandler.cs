using StackExchange.Redis;

namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe
{
    public interface IMessageHandler
    {
        RedisChannel Channel { get; }

        void HandleMessage(Message message);
        void CheckMissedMessages();
    }
}