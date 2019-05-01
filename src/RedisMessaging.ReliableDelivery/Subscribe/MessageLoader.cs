using System.Collections.Generic;
using StackExchange.Redis;

namespace RedisMessaging.ReliableDelivery.Subscribe
{
    public class MessageLoader : IMessageLoader
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;

        private static readonly LuaScript _getMessagesScript = LuaScript.Prepare("" +
                                                                                 "local channel = @channel\n" +
                                                                                 "local fromId = @fromId\n" +
                                                                                 "local toId = @toId\n" +
                                                                                 "local result = {}\n" +
                                                                                 "for id = fromId, toId, 1\n" +
                                                                                 "do\n" +
                                                                                 "    local messageContent = redis.call('GET', 'ch:{'..channel..'}:'..id)\n" +
                                                                                 "    if messageContent ~= false then\n" +
                                                                                 "        table.insert(result, id)\n" +
                                                                                 "        table.insert(result, messageContent)\n" +
                                                                                 "    end\n" +
                                                                                 "end\n" +
                                                                                 "return result");

        public MessageLoader(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
        }

        public IEnumerable<Message> GetMessages(string channelName, long fromMessageId, long toMessageId)
        {
            // TODO get messages in batches

            object parameters = new
            {
                channel = (RedisKey) channelName,
                fromId = fromMessageId,
                toId = toMessageId
            };
            var results = _connectionMultiplexer.GetDatabase().ScriptEvaluate(_getMessagesScript, parameters);

            int i = 0;
            long id = 0;
            foreach (var result in (RedisResult[])results)
            {
                if (i % 2 == 0)
                {
                    id = (long) result;
                }
                else
                {
                    var content = (string) result;
                    yield return new Message(id, content);
                }
                ++i;
            }
        }
    }
}