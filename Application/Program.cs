using Application;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Balancer;
using Grpc.Net.Client.Configuration;
using RecurrentWorkerService.Distributed.EtcdPersistence.Registration;
using RecurrentWorkerService.Distributed.Prioritization.Registration;
using RecurrentWorkerService.Distributed.Registration;

var factory = new StaticResolverFactory(addr => new[]
{
	new BalancerAddress("10.16.17.139", 12379),
	new BalancerAddress("10.16.17.139", 22379),
	new BalancerAddress("10.16.17.139", 32379),
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
			"LocalWorkerService",
			w =>
			{
				/*
				w.AddDistributedCronWorker<CronWorker>(
				    "CronWorker-1",
				    s => s
				        .SetCronExpression("* * * * *")
				        .SetRetryOnFailDelay(TimeSpan.FromSeconds(1)));
				*/

				
				w.AddDistributedRecurrentWorker<RecurrentWorker>(
				    "RecurrentWorker-1",
				    s => s.SetPeriod(TimeSpan.FromSeconds(20)).SetRetryOnFailDelay(TimeSpan.Zero));
				

				/*
				w.AddDistributedWorkloadWorker<WorkloadWorker>(
					"WorkloadWorker-2",
					s => s.SetRange(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10))
						.SetStrategies(c => c.Add(0, TimeSpan.FromSeconds(1))
							.Subtract(200, TimeSpan.FromSeconds(1))));
				*/
			})
			.AddEtcdPersistence(channel)
			.AddBasicPrioritization();
	})
	.Build()
	.RunAsync();
