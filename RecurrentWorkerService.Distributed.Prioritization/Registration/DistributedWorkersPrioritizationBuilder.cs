using Microsoft.Extensions.DependencyInjection;
using RecurrentWorkerService.Distributed.Prioritization.Indicators;

namespace RecurrentWorkerService.Distributed.Prioritization.Registration;

public class DistributedWorkersPrioritizationBuilder : IDistributedWorkersPrioritizationBuilder
{
	public IServiceCollection Services { get; }

	public DistributedWorkersPrioritizationBuilder(IServiceCollection services)
	{
		Services = services;
	}

	public DistributedWorkersPrioritizationBuilder AddNodePriorityIndicator<TPriorityIndicator>()
		where TPriorityIndicator : class, IPriorityIndicator
	{
		Services.AddTransient<TPriorityIndicator>();
		Services.AddTransient(c => new NodePriorityIndicator(c.GetRequiredService<TPriorityIndicator>()));

		return this;
	}

	public DistributedWorkersPrioritizationBuilder AddNodePriorityIndicator(Func<IServiceProvider, IPriorityIndicator> implementationFactory)
	{
		Services.AddTransient(c => new NodePriorityIndicator(implementationFactory(c)));

		return this;
	}

	public DistributedWorkersPrioritizationBuilder AddIdentityPriorityIndicator<TPriorityIndicator>(string identity)
		where TPriorityIndicator : class, IPriorityIndicator
	{
		Services.AddTransient<TPriorityIndicator>();
		Services.AddTransient(c => new IdentityPriorityIndicator(c.GetRequiredService<TPriorityIndicator>(), identity));

		return this;
	}

	public DistributedWorkersPrioritizationBuilder AddIdentityPriorityIndicator(string identity, Func<IServiceProvider, IPriorityIndicator> implementationFactory)
	{
		Services.AddTransient(c => new IdentityPriorityIndicator(implementationFactory(c), identity));

		return this;
	}
}