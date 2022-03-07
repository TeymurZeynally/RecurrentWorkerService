using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Balancer;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecurrentWorkerService.Distributed.Interfaces.Persistence.Models;

namespace RecurrentWorkerService.Distributed.EtcdPersistence.Tests;

[TestClass]
public class UnitTest1
{
	private Persistence.EtcdPersistence _persistence;

	[TestInitialize]
	public void TestInitialize()
	{
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
				ServiceProvider = new ServiceCollection().AddSingleton<ResolverFactory>(factory)
					.BuildServiceProvider(),
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

		_persistence = new Persistence.EtcdPersistence(new NullLogger<Persistence.EtcdPersistence>(), channel, "test-service", 1500);
	}



	[TestMethod]
	public async Task TestMethod1()
	{
		var cts = new CancellationTokenSource();

		var identity = "IDDD";

		var result = await _persistence.SucceededAsync(identity, 1, TimeSpan.FromDays(401), cts.Token);

		await _persistence.WaitForOrderAsync(5, identity, result.Revision, cts.Token);


		result = await _persistence.SucceededAsync(identity, 2, TimeSpan.FromDays(401), cts.Token);


		await _persistence.WaitForOrderAsync(5, identity, result.Revision, cts.Token);


		//
		//await _persistence.HeartbeatAsync(TimeSpan.FromSeconds(10), cts.Token);
		//await _persistence.AcquireExecutionLockAsync("2", cts.Token);

		//
		
		var revision = await _persistence.UpdateWorkloadAsync("adasd", new WorkloadInfo(), TimeSpan.FromDays(040), cts.Token);
	   await _persistence.GetCurrentWorkloadAsync("adasd", CancellationToken.None);

		string.GetHashCode("");
	}
}