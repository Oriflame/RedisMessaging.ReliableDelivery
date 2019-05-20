# Oriflame.RedisMessaging.ReliableDelivery

This library is written in .NET standard and provides reliability to delivering messages via Redis. By design Redis pub/sub message delivery is not reliable so it can happen that some messages can get lost due to network issues or they can be delivered more than once in case of Redis replication failure.

This library adds a lightweight infrastructure to handle such error scenarios.

## Download Nuget package

[![Nuget](https://img.shields.io/nuget/v/Oriflame.RedisMessaging.ReliableDelivery.svg?color=%2308f&label=Nuget)](https://www.nuget.org/packages/Oriflame.RedisMessaging.ReliableDelivery/)
![Nuget](https://img.shields.io/nuget/dt/Oriflame.RedisMessaging.ReliableDelivery.svg?label=Downloads&color=%2308f)

## How to Use It
```csharp
// initialization
var connectionMultiplexer = ConnectionMultiplexer.Connect("connection options");

// create reliable subscriber
var messageHandler = new MessageHandler(
    channel: "channel-name",
    onSuccessMessage: message =>
    {
        Trace.TraceInformation($"Message with ID={message.Id} was received as expected. Message content='{message.Content}'");
    },
    messageValidator: new MessageValidator(),
    messageLoader: new MessageLoader(connectionMultiplexer));

connectionMultiplexer.GetSubscriber().AddReliableSubscriber(
    messageHandler: messageHandler,
    messageParser: new MessageParser());

// create reliable publisher
var messageExpiration = TimeSpan.FromMinutes(10);
var reliablePublisher = connectionMultiplexer.GetSubscriber().AddReliablePublisher(messageExpiration);

// publishing a message
reliablePublisher.Publish("channel-name", "message-content");

// TBD Check message handler integrity
```

## How It Works


## Development
[![Build status](https://oriflame.visualstudio.com/Ori.Common/_apis/build/status/Redis/RedisMessaging.ReliableDelivery-CD?label=Release+build&branchName=master)](https://oriflame.visualstudio.com/Ori.Common/_build/latest?definitionId=1324&branchName=master)

[![Build status](https://oriflame.visualstudio.com/Ori.Common/_apis/build/status/Redis/RedisMessaging.ReliableDelivery-CD?label=Prerelease+build&branchName=develop)](https://oriflame.visualstudio.com/Ori.Common/_build/latest?definitionId=1324&branchName=develop)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/Oriflame.RedisMessaging.ReliableDelivery.svg?color=%2308f&label=Nuget)
