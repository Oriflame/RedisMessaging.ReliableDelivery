using System.Threading.Tasks;

namespace Oriflame.RedisMessaging.ReliableDelivery.Publish
{
    public interface IReliablePublisher
    {
        void Publish(string channel, string message);

        Task PublishAsync(string channel, string message);
    }
}