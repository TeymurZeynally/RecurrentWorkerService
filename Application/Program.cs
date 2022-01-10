using Application;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Balancer;
using Grpc.Net.Client.Configuration;
using RecurrentWorkerService.Distributed.EtcdPersistence.Registration;
using RecurrentWorkerService.Registration;

var factory = new StaticResolverFactory(addr => new[]
{
	new BalancerAddress("10.16.17.139", 12379),
	new BalancerAddress("10.16.17.139", 22379),
	new BalancerAddress("10.16.17.139", 32379),
});

await Host.CreateDefaultBuilder(args)
	.ConfigureServices(services =>
	{
		/*
		services.AddWorkers(w =>
		{
			w.AddRecurrentWorker<RecurrentWorker>(s => s.SetPeriod(TimeSpan.FromSeconds(1)));
			w.AddRecurrentWorker<RecurrentWorker>(s => s.SetPeriod(TimeSpan.FromSeconds(1)));
			w.AddRecurrentWorker<RecurrentWorker2>(s => s.SetPeriod(TimeSpan.FromSeconds(1)));
			w.AddRecurrentWorker<RecurrentWorker2>(s => s.SetPeriod(TimeSpan.FromSeconds(1)));
		});

		*/
		
		services.AddDistributedWorkers(
			"CoolService",
			GrpcChannel.ForAddress(
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
				}),
			w =>
			{

				w.AddDistributedRecurrentWorker<RecurrentWorker2>(
					"Worker-1",
					s => s.SetExecutionCount(1).SetPeriod(TimeSpan.FromSeconds(1)));

				w.AddDistributedRecurrentWorker<RecurrentWorker2>(
					"Worker-2",
					s => s.SetExecutionCount(1).SetPeriod(TimeSpan.FromSeconds(1)));
			});

	})
	.Build()
	.RunAsync();
