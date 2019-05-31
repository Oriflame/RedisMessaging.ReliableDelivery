namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe
{
    internal interface IMessageProcessor
    {
        void ProcessMessage(Message message, string channel);
    }
}