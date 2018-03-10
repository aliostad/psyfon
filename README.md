# Psyfon
A High-Performance Azure EventHubs Sink capable of buffering/batching and regularly commiting your events. It supports .NET Standard 2.0 (netstandard2.0) and .NET 4.52 (net452).

# Getting Started
First install the package:
``` powershell
Install-Package Psyfon
```
Then create a single instance for the application/process:
``` csharp
var dispatcher = new BufferingEventDispatcher("<YOUR EVENTHUB CONNECTION STRING>"); // make sure specify TransportType=Amqp
```
And in your application, simply create `EvenetData` and `Add` (optionally send a PartitionKey):

```
var ed = new EventData(myEventSerialisedAsByteArray);
dispatcher.Add(ed); // you may also supply an optional PartitionKey string.
```

That is it!

# Optional Parameters

 - maxBatchSize: Maximum size of the batched EventData to send, default is 64KB. It can be up to 250KB. Depending on the size of your event change this value but values above 128KB not recommended.
 - maxSendIntervalSeconds: Maximum number of seconds before events are commited to EventHub even if batch is not full. Default is 5 seconds. Depending on the acceptable latencing in the application, increase or decrease.
 - hasher: An implementation of `IHasher` which uniformly hashes the PartitionKeys you supply. By default uses MD5 which is very fast. While MD5 is cryprtographically broken, it produces uniform hashes and suitable for our purpose.
 - logger: An `Action<TraceLevel, string>`. By default it uses `Trace`.


# How it works
Psyfon creates a partition sender per partition, each running in its own dedicated thread. This is the fastest way to send events to the Azure EventHub. That is why it has to allocate events to partitions using a uniform hashing of the PartitionKey.

Batches get retried 3 times.
