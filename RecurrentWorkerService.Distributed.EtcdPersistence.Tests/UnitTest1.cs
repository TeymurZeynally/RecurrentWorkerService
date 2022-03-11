using System;
using System.Diagnostics;
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

		_persistence = new Persistence.EtcdPersistence(new NullLogger<Persistence.EtcdPersistence>(), channel, "test-service", 1501);
	}



	[TestMethod]
	public async Task TestMethod1()
	{
		var task = Task.Run(async () =>
		{
			try
			{
				await foreach (var update in _persistence.WatchPriorityUpdates(CancellationToken.None))
				{
					Debug.Print($"Received priority update {update.NodeId} {update.Identity} {(update.Priority.HasValue ? update.Priority.Value.ToString() : "NO")}");
				}
			}
			catch (Exception e)
			{
			}
		});

		var rand = new Random();
		await _persistence.HeartbeatAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
		await _persistence.UpdatePriorityAsync("ID", (byte)rand.Next(byte.MinValue, byte.MaxValue), CancellationToken.None);



		await Task.WhenAny(task);
	}
}