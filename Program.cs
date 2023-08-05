using Grpc.Core;
using Grpc.Net.Client;
using SlimFeed.Protocol;

using var channel = GrpcChannel.ForAddress(args[0]);
var cancellationTokenSource = new CancellationTokenSource();

// Connect
var client = new SlimFeed.Protocol.SlimFeed.SlimFeedClient(channel);
var serverInfo = await client.InitializeAsync(new SlimFeed.Protocol.ClientInfo { Name = "SlimClient"});
Console.WriteLine($"Connected to {serverInfo.Name}");

// Start subscription requests
using (var subscription = client.Subscribe())
{
    foreach (string id in new[] { "BMW", "Tesla", "Toyota" })
    {
        Console.WriteLine($"Subscribing {id}");
        await subscription.RequestStream.WriteAsync(new SlimFeed.Protocol.SubscriptionRequest { InstrumentId = id });
    }

    // Process subscription responses
    while (await subscription.ResponseStream.MoveNext(cancellationTokenSource.Token))
    {
        SubscriptionResponse response = subscription.ResponseStream.Current;
        Console.WriteLine(response switch
        {
            { HasError: true } => response.Error,
            _ => $"Tick bid={response.Bid}, ask={response.Ask}, last={response.Last}"
        });
    }
}

Console.WriteLine("Press any key to exit...");
Console.ReadKey();