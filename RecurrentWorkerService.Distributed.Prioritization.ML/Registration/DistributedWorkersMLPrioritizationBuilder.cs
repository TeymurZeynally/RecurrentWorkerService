using Microsoft.Extensions.DependencyInjection;
using RecurrentWorkerService.Distributed.Prioritization.ML.Indicators;
using RecurrentWorkerService.Distributed.Prioritization.ML.Indicators.ML;
using RecurrentWorkerService.Distributed.Prioritization.ML.Indicators.Models;
using RecurrentWorkerService.Distributed.Prioritization.ML.Registration.Models;
using RecurrentWorkerService.Distributed.Prioritization.ML.Repository;
using RecurrentWorkerService.Distributed.Prioritization.Registration;

namespace RecurrentWorkerService.Distributed.Prioritization.ML.Registration;

public class DistributedWorkersMLPrioritizationBuilder
{
	private readonly IDistributedWorkersPrioritizationBuilder _distributedWorkersPrioritizationBuilder;

	public DistributedWorkersMLPrioritizationBuilder(IDistributedWorkersPrioritizationBuilder distributedWorkersPrioritizationBuilder)
	{
		_distributedWorkersPrioritizationBuilder = distributedWorkersPrioritizationBuilder;
	}

	public DistributedWorkersMLPrioritizationBuilder AddIdentityPriorityIndicator(string identity, Influence cpu, Influence ram, Influence network)
	{
		_distributedWorkersPrioritizationBuilder.AddIdentityPriorityIndicator(
			identity,
			f => new MLPriorityIndicator(
				new MetricsInfluence { Cpu = cpu.ToPercent(), Memory = ram.ToPercent(), Network = network.ToPercent() },
				f.GetRequiredService<PriorityModel>(),
				f.GetRequiredService<IMetricsRepository>()));

		return this;
	}
}