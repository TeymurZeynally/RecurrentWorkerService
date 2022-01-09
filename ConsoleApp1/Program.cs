using Google.Protobuf;
using Grpc.Net.Client;


using var channel = GrpcChannel.ForAddress("http://localhost:2379");
var lockClient = new V3Lockpb.Lock.LockClient(channel);
var leaseClient = new Etcdserverpb.Lease.LeaseClient(channel);

var lease = leaseClient.LeaseGrant(new Etcdserverpb.LeaseGrantRequest() { TTL = 10 });

var leaseer = leaseClient.LeaseKeepAlive(); 
Task.Run(async () => { 
	while (true) {
		await leaseer.RequestStream.WriteAsync(new Etcdserverpb.LeaseKeepAliveRequest { ID = lease.ID });
		var _ = Task.Run(async () =>
		{
			while (await leaseer.ResponseStream.MoveNext(CancellationToken.None))
			{
				Console.WriteLine(leaseer.ResponseStream.Current);
			}
		});
		Console.WriteLine("ITS ALLIVE!");
		await Task.Delay(TimeSpan.FromSeconds(5));
	}
});


Console.WriteLine(lease);
var result = lockClient.Lock(new V3Lockpb.LockRequest { Name = ByteString.CopyFromUtf8("1234"), Lease = lease.ID });
Console.WriteLine(result);
Console.WriteLine("Press any key to exit...");
Console.ReadKey();