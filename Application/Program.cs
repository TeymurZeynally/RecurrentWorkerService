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
		/*
		services.AddWorkers(w =>
		{
			// Direct type registration
			w.AddCronWorker<ExampleOfCronWorker>(s => s
				.SetCronExpression("* * * * *")
				.SetRetryOnFailDelay(TimeSpan.FromSeconds(1)));

			w.AddRecurrentWorker<ExampleOfRecurrentWorker>(s => s
				.SetPeriod(TimeSpan.FromSeconds(1))
				.SetRetryOnFailDelay(TimeSpan.FromMilliseconds(10)));

			w.AddWorkloadWorker<ExampleOfWorkloadWorker>(s => s
				.SetRange(TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5))
				.SetStrategies(c => c
					.Add(Workload.Zero, TimeSpan.FromSeconds(30))
					.Multiply(Workload.FromPercent(10), 2)
					.Add(Workload.FromPercent(25), TimeSpan.FromSeconds(10))
					.Subtract(Workload.FromPercent(50), TimeSpan.FromSeconds(30))
					.Divide(Workload.FromPercent(80), 2d)
					.Set(Workload.Full, TimeSpan.FromSeconds(1)))
				.SetRetryOnFailDelay(TimeSpan.FromSeconds(1)));

			// Registration of implementation factories
			w.AddCronWorker(
				c => new ExampleOfCronWorker(c.GetRequiredService<ILogger<ExampleOfCronWorker>>()),
				s => s
					.SetCronExpression("* * * * *")
					.SetRetryOnFailDelay(TimeSpan.FromSeconds(1)));

			w.AddRecurrentWorker(
				c => new ExampleOfRecurrentWorker(c.GetRequiredService<ILogger<ExampleOfRecurrentWorker>>()),
				s => s
					.SetPeriod(TimeSpan.FromSeconds(1))
					.SetRetryOnFailDelay(TimeSpan.FromMilliseconds(10)));

			w.AddWorkloadWorker(
				c => new ExampleOfWorkloadWorker(c.GetRequiredService<ILogger<ExampleOfWorkloadWorker>>()),
				s => s
					.SetRange(TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5))
					.SetStrategies(c => c
						.Add(Workload.Zero, TimeSpan.FromSeconds(1))
						.Set(Workload.Full, TimeSpan.FromSeconds(1)))
					.SetRetryOnFailDelay(TimeSpan.FromSeconds(1)));

			// Registration with config sections
			w.AddCronWorker(
				c => new ExampleOfCronWorker(c.GetRequiredService<ILogger<ExampleOfCronWorker>>()),
				s => s.FromConfigSection(context.Configuration.GetRequiredSection("WorkerSchedules:ExampleOfSomeCronSchedule")));

			w.AddRecurrentWorker(
				c => new ExampleOfRecurrentWorker(c.GetRequiredService<ILogger<ExampleOfRecurrentWorker>>()),
				s => s.FromConfigSection(context.Configuration.GetRequiredSection("WorkerSchedules:ExampleOfSomeRecurrentSchedule")));

			w.AddWorkloadWorker(
				c => new ExampleOfWorkloadWorker(c.GetRequiredService<ILogger<ExampleOfWorkloadWorker>>()),
				s => s.FromConfigSection(context.Configuration.GetRequiredSection("WorkerSchedules:ExampleOfSomeWorkloadSchedule")));

			w.AddWorkloadWorker(
				c => new ExampleOfWorkloadWorker(c.GetRequiredService<ILogger<ExampleOfWorkloadWorker>>()),
				s => s.FromConfigSection(context.Configuration.GetRequiredSection("WorkerSchedules:ExampleOfSomeWorkloadScheduleWithStrategies")));
		});
		*/
		services.AddDistributedWorkers(
			"LocalWorkerService",
			w =>
			{
				/*
				w.AddDistributedCronWorker<ExampleOfCronWorker>(
					"CronWorker-1",
					s => s
						.SetCronExpression("* * * * *")
						.SetRetryOnFailDelay(TimeSpan.FromSeconds(1)));

				w.AddDistributedCronWorker(
					"CronWorker-2",
					c => new ExampleOfCronWorker(c.GetRequiredService<ILogger<ExampleOfCronWorker>>()),
				    s => s
				        .SetCronExpression("* * * * *")
				        .SetRetryOnFailDelay(TimeSpan.FromSeconds(1)));
				*/

				w.AddDistributedRecurrentWorker<ExampleOfRecurrentWorker>(
					"RecurrentWorker-1",
					s => s
						.SetPeriod(TimeSpan.FromSeconds(5))
						.SetRetryOnFailDelay(TimeSpan.Zero));

				/*
				w.AddDistributedWorkloadWorker<ExampleOfWorkloadWorker>(
					"WorkloadWorker-1",
					s => s
						.SetRange(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10))
						.SetStrategies(c => c
							.Add(Workload.Zero, TimeSpan.FromSeconds(1))
							.Subtract(Workload.FromPercent(50), TimeSpan.FromSeconds(1))));
				*/
			})
			.AddEtcdPersistence(channel)
			.AddPrioritization(o =>
			{
				// o.AddIdentityPriorityIndicator<PriorityIndicator>("RecurrentWorker-1");

				o.AddMLPrioritization(@"C:\ML", c => c.BaseAddress = new Uri("http://localhost:8083"), m =>
				{
					m.AddIdentityPriorityIndicator("RecurrentWorker-1", Influence.Zero, Influence.Full, Influence.Full);
				});
			});
	})
	.Build()
	.RunAsync()
	.ConfigureAwait(false);
