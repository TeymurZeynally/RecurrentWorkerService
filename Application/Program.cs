using Application;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Balancer;
using Grpc.Net.Client.Configuration;
using RecurrentWorkerService.Distributed.EtcdPersistence.Registration;
using RecurrentWorkerService.Distributed.Prioritization.ML.Registration;
using RecurrentWorkerService.Distributed.Prioritization.ML.Registration.Models;
using RecurrentWorkerService.Distributed.Prioritization.Registration;
using RecurrentWorkerService.Distributed.Registration;
using RecurrentWorkerService.Registration;
using RecurrentWorkerService.Workers.Models;

var factory = new StaticResolverFactory(addr => new[]
{
	new BalancerAddress("localhost", 23791),
	new BalancerAddress("localhost", 23792),
	new BalancerAddress("localhost", 23793),
});

var channel = GrpcChannel.ForAddress(
	"static://",
	new GrpcChannelOptions
	{
		Credentials = ChannelCredentials.Insecure,
		ServiceProvider = new ServiceCollection().AddSingleton<ResolverFactory>(factory).BuildServiceProvider(),
		ServiceConfig = new ServiceConfig
		{
			MethodConfigs =
			{
				new MethodConfig
				{
					Names = { MethodName.Default },
					RetryPolicy = new RetryPolicy
					{
						MaxAttempts = 5,
						InitialBackoff = TimeSpan.FromSeconds(1),
						MaxBackoff = TimeSpan.FromSeconds(5),
						BackoffMultiplier = 1.5,
						RetryableStatusCodes = { StatusCode.Unavailable }
					}
				}
			}
		}
	});


await Host.CreateDefaultBuilder(args)
	.ConfigureServices((context, services) =>
	{
		services.AddDistributedWorkers(
			"LocalWorkerService",
			w =>
			{
				w.AddDistributedWorkloadWorker<CpuLoadWorker>(
					"Workload-CPU-1",
					s => s
						.SetRange(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(40))
						.SetStrategies(st => st
							.Add(Workload.FromPercent(0), TimeSpan.FromSeconds(1))
							.Subtract(Workload.FromPercent(50), TimeSpan.FromSeconds(1)))
						.SetRetryOnFailDelay(TimeSpan.Zero));

				w.AddDistributedWorkloadWorker<CpuLoadWorker>(
					"Workload-CPU-2",
					s => s
						.SetRange(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(50))
						.SetStrategies(st => st
							.Add(Workload.FromPercent(0), TimeSpan.FromSeconds(3))
							.Subtract(Workload.FromPercent(50), TimeSpan.FromSeconds(3)))
						.SetRetryOnFailDelay(TimeSpan.Zero));

				w.AddDistributedWorkloadWorker<MemoryLoadWorker>(
					"Workload-Mem-1",
					s => s
						.SetRange(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(40))
						.SetStrategies(st => st
							.Add(Workload.FromPercent(0), TimeSpan.FromSeconds(1))
							.Subtract(Workload.FromPercent(50), TimeSpan.FromSeconds(1)))
						.SetRetryOnFailDelay(TimeSpan.Zero));

				w.AddDistributedWorkloadWorker<MemoryLoadWorker>(
					"Workload-Mem-1",
					s => s
						.SetRange(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(50))
						.SetStrategies(st => st
							.Add(Workload.FromPercent(0), TimeSpan.FromSeconds(3))
							.Subtract(Workload.FromPercent(50), TimeSpan.FromSeconds(3)))
						.SetRetryOnFailDelay(TimeSpan.Zero));

				w.AddDistributedWorkloadWorker<NetLoadWorker>(
					"Workload-Net-1",
					s => s
						.SetRange(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(40))
						.SetStrategies(st => st
							.Add(Workload.FromPercent(0), TimeSpan.FromSeconds(1))
							.Subtract(Workload.FromPercent(50), TimeSpan.FromSeconds(1)))
						.SetRetryOnFailDelay(TimeSpan.Zero));

				w.AddDistributedWorkloadWorker<NetLoadWorker>(
					"Workload-Net-1",
					s => s
						.SetRange(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(50))
						.SetStrategies(st => st
							.Add(Workload.FromPercent(0), TimeSpan.FromSeconds(4))
							.Subtract(Workload.FromPercent(50), TimeSpan.FromSeconds(4)))
						.SetRetryOnFailDelay(TimeSpan.Zero));
			})
			.AddEtcdPersistence(channel);
	})
	.Build()
	.RunAsync()
	.ConfigureAwait(false);
