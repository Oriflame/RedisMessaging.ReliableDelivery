﻿using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Oriflame.RedisMessaging.ReliableDelivery.Publish
{
    public class ReliablePublisher : IReliablePublisher
    {
        public const char MessagePartSeparator = ':';
        private static readonly LuaScript _publishScript = LuaScript.Prepare(TrimScript(""
                                         + "local message = @message\n"
                                         + "local channel = @channel\n"
                                         + "local expiration = @expiration\n"
                                         + "local message_id = redis.call('INCR','ch:{'..channel..'}:id')\n"
                                         + "local msg = message_id..'" + MessagePartSeparator + "'..message\n"
                                         + "local clusterNodes = redis.pcall('CLUSTER', 'NODES')\n"
                                         + "redis.call('SET', 'ch:{'..channel..'}:'..message_id, message, 'EX', expiration)\n"
                                         // If a variable 'clusterNodes' is a string variable then command 'cluster nodes' did not fail
                                         // hence the server mode is clustered.
                                         // If it fails (only if server mode=standalone) then a variable 'clusterNodes' is an error object
                                         + "if type(clusterNodes) ~= 'string' or string.match(clusterNodes, 'myself,master') then\n"
                                         //    publishing a message only if this script is executed in master node
                                         //    We do not want the message to be published again by slave node(s).
                                         + "   redis.call('PUBLISH',channel,msg)\n"
                                         + "end\n"
                                         ));

        private readonly IConnectionMultiplexer _connectionMultiplexer;

        protected virtual IDatabase Database => _connectionMultiplexer.GetDatabase();

        public ReliablePublisher(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
        }

        public TimeSpan MessageExpiration { get; set; } = TimeSpan.FromMinutes(10);

        public void Publish(string channel, string message)
        {
            var parameters = CreateScriptParameters(channel, message);
            Database.ScriptEvaluate(_publishScript, parameters, CommandFlags.DemandMaster | CommandFlags.PreferMaster | CommandFlags.NoRedirect);
        }

        public Task PublishAsync(string channel, string message)
        {
            var parameters = CreateScriptParameters(channel, message);
            return Database.ScriptEvaluateAsync(_publishScript, parameters, CommandFlags.DemandMaster);
        }

        private object CreateScriptParameters(string channel, string message)
        {
            return new
            {
                channel = (RedisKey)channel,
                message = message,
                expiration = (int)MessageExpiration.TotalSeconds
            };
        }

        private static string TrimScript(string script)
        {
            return Regex.Replace(script, @" {2,}", "")
                .Trim('\r', '\n');
        }
    }
}