using System.Collections.Generic;

namespace RedisMessaging.ReliableDelivery.Subscribe
{
    public interface IMessageLoader
    {
        IEnumerable<Message> GetMessages(string channelName, long fromMessageId, long toMessageId);
    }
}