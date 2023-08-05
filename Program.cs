using Grpc.Core;
using Grpc.Net.Client;
using SlimFeed.Protocol;

using var channel = GrpcChannel.ForAddress(args[0]);
var cancellationTokenSource = new CancellationTokenSource();

// Connect
var client = new SlimFeed.Protocol.SlimFeed.SlimFeedClient(channel);
var serverInfo = await client.InitializeAsync(new SlimFeed.Protocol.ClientInfo { Name = "SlimClient"});
Console.WriteLine($"Connected to {serverInfo.Name}");

using (var subscription = client.Subscribe())
{
    var instrumentIds = new[] { "BMW", "Tesla", "Toyota" };

    // Start subscription requests
    foreach (string id in instrumentIds)
    {
        Console.WriteLine($"Subscribing {id}");
        await subscription.RequestStream.WriteAsync(new SlimFeed.Protocol.SubscriptionRequest { InstrumentId = id, Type = SubscriptionType.Add });
    }

    // Process subscription responses
    int responseCount = 0;
    while (responseCount < 10 && await subscription.ResponseStream.MoveNext(cancellationTokenSource.Token))
    {
        responseCount++;
        SubscriptionResponse response = subscription.ResponseStream.Current;
        Console.WriteLine($"{response.InstrumentId}: " + response switch
        {
            { HasError: true } => response.Error,
            _ => $"bid={response.Bid}, ask={response.Ask}, last={response.Last}, currency={response.Currency}"
        });
    }

    Console.WriteLine($"Stopping subscriptions after {responseCount} responses.");

    // Stop subscriptions
    foreach (string id in instrumentIds)
    {
        Console.WriteLine($"Unsubscribing {id}");
        await subscription.RequestStream.WriteAsync(new SlimFeed.Protocol.SubscriptionRequest { InstrumentId = id, Type = SubscriptionType.Remove });
    }
}

Console.WriteLine("Press any key to exit...");
Console.ReadKey();