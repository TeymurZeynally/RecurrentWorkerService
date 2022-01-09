using Application;
using dotnet_etcd;
using Etcdserverpb;
using Grpc.Core;
using RecurrentWorkerService.Distributed.RedisPersistence.Registration;
using RecurrentWorkerService.Registration;
using StackExchange.Redis;






EtcdClient client = new EtcdClient("http://localhost:2379");


Console.WriteLine(client.GetVal("foo"));
var result =  client.Lock("mylock1");
Console.Write("OK" + result);
Console.ReadKey();





IHost host = Host.CreateDefaultBuilder(args)
	.ConfigureServices(services =>
	{
		services.AddDistributedWorkers(
			"CoolService",
			ConnectionMultiplexer.Connect("localhost").GetDatabase(),
			w =>
			{
				w.AddDistributedRecurrentWorker<RecurrentWorker>(
					"Worker-1", 
					s => s.SetExecutionCount(1).SetPeriod(TimeSpan.FromSeconds(1)));
			});
	})
	.Build();

await host.RunAsync();
