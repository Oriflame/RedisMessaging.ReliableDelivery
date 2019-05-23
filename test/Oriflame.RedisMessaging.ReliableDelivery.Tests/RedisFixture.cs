using System;
using StackExchange.Redis;

namespace Oriflame.RedisMessaging.ReliableDelivery.Tests
{
    public class RedisFixture : IDisposable
    {
        private IConnectionMultiplexer _connectionMultiplexer;
        private readonly object _lock;

        public RedisFixture()
        {
            RedisInside = new RedisInside.Redis();
            _lock = new object();
            _connectionMultiplexer = null;
        }

        public IConnectionMultiplexer GetConnection()
        {
            if (_connectionMultiplexer == null)
            {
                lock (_lock)
                {
                    if (_connectionMultiplexer == null)
                    {
                        _connectionMultiplexer = CreateMultiplexer();
                    }
                }
            }

            return _connectionMultiplexer;
        }

        public IConnectionMultiplexer CreateMultiplexer()
        {
            return ConnectionMultiplexer.Connect(
                new ConfigurationOptions
                {
                    EndPoints = {RedisInside.Endpoint},
                    ClientName = nameof(RedisFixture),
                    AllowAdmin = true
                });
        }

        public RedisInside.Redis RedisInside { get; }

        public IDatabase GetDatabase(int db = -1, object asyncState = null) => GetConnection().GetDatabase(db, asyncState);

        public IServer GetServer() => GetConnection().GetServer(RedisInside.Endpoint);

        public void Dispose()
        {
            _connectionMultiplexer?.Dispose();
            RedisInside?.Dispose();
        }
    }
}