using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using ExperimentalApplication.Payloads;
using ExperimentalApplication.Workers;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Balancer;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using RecurrentWorkerService.Distributed.EtcdPersistence.Registration;
using RecurrentWorkerService.Distributed.Prioritization.Registration;
using RecurrentWorkerService.Distributed.Registration;

using OpenTelemetry.Trace;
using RecurrentWorkerService.Workers.Models;

HttpClient.DefaultProxy = new WebProxy();

var factory = new StaticResolverFactory(addr => new[]
{
	new BalancerAddress("10.16.17.139", 23791),
	new BalancerAddress("10.16.17.139", 23792),
	new BalancerAddress("10.16.17.139", 23793),
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
						RetryableStatusCodes = { Grpc.Core.StatusCode.Unavailable }
					}
				}
			}
		}
	});


var source = new ActivitySource(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name!);
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
	.AddSource(
		source.Name,
		"RecurrentWorkerService.Distributed",
		"RecurrentWorkerService.Distributed.EtcdPersistence",
		"RecurrentWorkerService.Distributed.Prioritization")
	.AddConsoleExporter()
	.AddOtlpExporter(opt =>
	{
		opt.Endpoint = new Uri("http://10.16.17.139:4317");
		opt.Protocol = OtlpExportProtocol.Grpc;
		opt.BatchExportProcessorOptions = new BatchExportProcessorOptions<Activity>
		{
			MaxExportBatchSize = 10000,
			ScheduledDelayMilliseconds = 2000,
			MaxQueueSize = 1_000_000
		};
	})
	.Build();


var nodeId = Math.Abs(BitConverter.ToInt64(RandomNumberGenerator.GetBytes(64)));

await Host.CreateDefaultBuilder(args)
	.ConfigureServices(services =>
	{
		services.AddSingleton(source);

		services.AddDistributedWorkers(
			"LocalWorkerService",
			nodeId,
			w =>
			{
				w.AddDistributedCronWorker(
					"Cron-Immediate",
					c => new CronWorker(
						new ImmediatePayload(c.GetRequiredService<ActivitySource>(), nodeId, "Cron-Immediate"),
						c.GetRequiredService<ILogger<CronWorker>>()),
					s => s
						.SetCronExpression("* * * * *")
						.SetRetryOnFailDelay(TimeSpan.FromSeconds(1)));

				w.AddDistributedCronWorker(
					"Cron-Fast",
					c => new CronWorker(
						new FastPayload(c.GetRequiredService<ActivitySource>(), nodeId, "Cron-Fast"),
						c.GetRequiredService<ILogger<CronWorker>>()),
					s => s
						.SetCronExpression("* * * * *")
						.SetRetryOnFailDelay(TimeSpan.FromSeconds(1)));

				w.AddDistributedCronWorker(
					"Cron-Slow",
					c => new CronWorker(
						new SlowPayload(c.GetRequiredService<ActivitySource>(), nodeId, "Cron-Slow"),
						c.GetRequiredService<ILogger<CronWorker>>()),
					s => s
						.SetCronExpression("* * * * *")
						.SetRetryOnFailDelay(TimeSpan.FromSeconds(1)));

				w.AddDistributedCronWorker(
					"Cron-Problem",
					c => new CronWorker(
						new ProblemPayload(10, c.GetRequiredService<ActivitySource>(), nodeId, "Cron-Problem"),
						c.GetRequiredService<ILogger<CronWorker>>()),
					s => s
						.SetCronExpression("* * * * *")
						.SetRetryOnFailDelay(TimeSpan.FromSeconds(1)));



				w.AddDistributedRecurrentWorker(
					"Recurrent-Immediate",
					c => new RecurrentWorker(
						new ImmediatePayload(c.GetRequiredService<ActivitySource>(), nodeId, "Recurrent-Immediate"),
						c.GetRequiredService<ILogger<RecurrentWorker>>()),
					s => s
						.SetPeriod(TimeSpan.FromTicks(1))
						.SetRetryOnFailDelay(TimeSpan.FromSeconds(1)));

				w.AddDistributedRecurrentWorker(
					"Recurrent-Fast",
					c => new RecurrentWorker(
						new FastPayload(c.GetRequiredService<ActivitySource>(), nodeId, "Recurrent-Fast"),
						c.GetRequiredService<ILogger<RecurrentWorker>>()),
					s => s
						.SetPeriod(TimeSpan.FromSeconds(1))
						.SetRetryOnFailDelay(TimeSpan.FromSeconds(1)));

				w.AddDistributedRecurrentWorker(
					"Recurrent-Slow",
					c => new RecurrentWorker(
						new SlowPayload(c.GetRequiredService<ActivitySource>(), nodeId, "Recurrent-Slow"),
						c.GetRequiredService<ILogger<RecurrentWorker>>()),
					s => s
						.SetPeriod(TimeSpan.FromSeconds(1))
						.SetRetryOnFailDelay(TimeSpan.FromSeconds(1)));

				w.AddDistributedRecurrentWorker(
					"Recurrent-Problem",
					c => new RecurrentWorker(
						new ProblemPayload(10, c.GetRequiredService<ActivitySource>(), nodeId, "Recurrent-Problem"),
						c.GetRequiredService<ILogger<RecurrentWorker>>()),
					s => s
						.SetPeriod(TimeSpan.FromSeconds(1))
						.SetRetryOnFailDelay(TimeSpan.FromSeconds(1)));




				w.AddDistributedWorkloadWorker(
					"Workload-Immediate",
					c => new WorkloadWorker(
						new ImmediatePayload(c.GetRequiredService<ActivitySource>(), nodeId, "Workload-Immediate"),
						c.GetRequiredService<ILogger<WorkloadWorker>>()),
					s => s
						.SetRange(TimeSpan.FromTicks(1), TimeSpan.FromTicks(1))
						.SetStrategies(c => c
							.Add(Workload.Zero, TimeSpan.FromSeconds(1))
							.Subtract(Workload.FromPercent(50), TimeSpan.FromSeconds(1))));

				w.AddDistributedWorkloadWorker(
					"Workload-Fast",
					c => new WorkloadWorker(
						new FastPayload(c.GetRequiredService<ActivitySource>(), nodeId, "Workload-Fast"),
						c.GetRequiredService<ILogger<WorkloadWorker>>()),
					s => s
						.SetRange(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10))
						.SetStrategies(c => c
							.Add(Workload.Zero, TimeSpan.FromSeconds(1))
							.Subtract(Workload.FromPercent(50), TimeSpan.FromSeconds(1))));

				w.AddDistributedWorkloadWorker(
					"Workload-Slow",
					c => new WorkloadWorker(
						new SlowPayload(c.GetRequiredService<ActivitySource>(), nodeId, "Workload-Slow"),
						c.GetRequiredService<ILogger<WorkloadWorker>>()),
					s => s
						.SetRange(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10))
						.SetStrategies(c => c
							.Add(Workload.Zero, TimeSpan.FromSeconds(1))
							.Subtract(Workload.FromPercent(50), TimeSpan.FromSeconds(1))));

				w.AddDistributedWorkloadWorker(
					"Workload-Problem",
					c => new WorkloadWorker(
						new ProblemPayload(10, c.GetRequiredService<ActivitySource>(), nodeId, "Workload-Problem"),
						c.GetRequiredService<ILogger<WorkloadWorker>>()),
					s => s
						.SetRange(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10))
						.SetStrategies(c => c
							.Add(Workload.Zero, TimeSpan.FromSeconds(1))
							.Subtract(Workload.FromPercent(50), TimeSpan.FromSeconds(1))));
			})
			.AddEtcdPersistence(channel)
			.AddBasicPrioritization();
	})
	.Build()
	.RunAsync();
