using System.Threading.Tasks;

namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe
{
    public interface IReliableSubscriber
    {
        void Subscribe(IMessageHandler handler);

        Task SubscribeAsync(IMessageHandler handler);

        void Unsubscribe(string channel);

        Task UnsubscribeAsync(string channel);

        void UnsubscribeAll();

        Task UnsubscribeAllAsync();
    }
}