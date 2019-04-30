using System;
using System.Threading.Tasks;

namespace RedisMessaging.ReliableDelivery.Subscribe
{
    public interface IReliableSubscriber
    {
        void Subscribe(string channel, Action<string, string> handler);

        Task SubscribeAsync(string channel, Action<string, string> handler);

        void Unsubscribe(string channel);

        Task UnsubscribeAsync(string channel);

        void UnsubscribeAll();

        Task UnsubscribeAllAsync();
    }
}