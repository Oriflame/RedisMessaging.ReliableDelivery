# Oriflame.RedisMessaging.ReliableDelivery

This library is written in .NET standard and provides reliability to delivering messages via Redis. By design Redis pub/sub message delivery is not reliable so it can happen that some messages can get lost due to network issues or they can be delivered more than once in case of Redis replication failure.

This library adds a lightweight infrastructure to handle such error scenarios.

## Download Nuget package

[![Nuget](https://img.shields.io/nuget/v/Oriflame.RedisMessaging.ReliableDelivery.svg?color=%2308f&label=Nuget)](https://www.nuget.org/packages/Oriflame.RedisMessaging.ReliableDelivery/)
![Nuget](https://img.shields.io/nuget/dt/Oriflame.RedisMessaging.ReliableDelivery.svg?label=Downloads&color=%2308f)

## How to Use It
```csharp
// initialization
var multiplexer = ConnectionMultiplexer.Connect("connection options");

// subscribe to a reliable channel
var channelIntegrityCheck = multiplexer.GetSubscriber()
    .Reliably()
    .Subscribe(
        channel: "channel-name",
        onExpectedMessage: (channel, message) => Trace.TraceInformation($"Message with ID={message.Id} was received as expected. Message content='{message.Content}'"),
        onMissedMessage:,
        onDuplicatedMessage:,
        onMissingMessages:);

// publish to a reliable channel
multiplexer.GetSubscriber()
    .Reliably()
    .Publish(
        channel: "channel-name",
        message: "message-content",
        messageExpiration: TimeSpan.FromMinutes(10));

// channel integrity check - optional
// useful when delivering messages is not as frequent. In such a case
// we can ensure manually that reliable subscriber is still able to receive messages.
// Example:
// if last processed message was earlier than 1sec ago
// i.e. we did not receive messages within the last second
if (channelIntegrityCheck.LastActivityAt < DateTime.UtcNow.AddSeconds(-1))
{
    // force reliable channel to get not processed messages
    // from Redis server if there are some
    channelIntegrityCheck.CheckForMissedMessages();
}
```

## How It Works
TODO

### Limitations
* Subscribing to channel patterns  is not supported

## Development
[![Build status](https://oriflame.visualstudio.com/Ori.Common/_apis/build/status/Redis/RedisMessaging.ReliableDelivery-CD?label=Release+build&branchName=master)](https://oriflame.visualstudio.com/Ori.Common/_build/latest?definitionId=1324&branchName=master)

[![Build status](https://oriflame.visualstudio.com/Ori.Common/_apis/build/status/Redis/RedisMessaging.ReliableDelivery-CD?label=Prerelease+build&branchName=develop)](https://oriflame.visualstudio.com/Ori.Common/_build/latest?definitionId=1324&branchName=develop)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/Oriflame.RedisMessaging.ReliableDelivery.svg?color=%2308f&label=Nuget)
