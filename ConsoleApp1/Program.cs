using Etcdserverpb;
using Google.Protobuf;
using Grpc.Net.Client;
using RecurrentWorkerService.Distributed.EtcdPersistence.Persistence;


using var channel = GrpcChannel.ForAddress("http://localhost:2379");

var service = Guid.NewGuid().ToString();
var etcd = new EtcdPersistence(channel, service, 100501);

await etcd.SucceededAsync("123", 45, TimeSpan.FromSeconds(10), CancellationToken.None);

while (true)
{
	Console.WriteLine(await etcd.IsSucceededAsync("123", 45, CancellationToken.None));	
}

//while (true)
//{
//	Console.WriteLine($"{DateTime.Now} OK");
//	await etcd.HeartbeatAsync(TimeSpan.FromSeconds(5));
//}
