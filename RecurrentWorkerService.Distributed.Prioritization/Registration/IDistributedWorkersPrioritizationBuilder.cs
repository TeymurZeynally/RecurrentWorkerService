using Microsoft.Extensions.DependencyInjection;
using RecurrentWorkerService.Distributed.Prioritization.Indicators;

namespace RecurrentWorkerService.Distributed.Prioritization.Registration;

public interface IDistributedWorkersPrioritizationBuilder
{
	public IServiceCollection Services { get; }

	DistributedWorkersPrioritizationBuilder AddNodePriorityIndicator<TPriorityIndicator>()
		where TPriorityIndicator : class, IPriorityIndicator;

	DistributedWorkersPrioritizationBuilder AddNodePriorityIndicator(Func<IServiceProvider, IPriorityIndicator> implementationFactory);

	DistributedWorkersPrioritizationBuilder AddIdentityPriorityIndicator<TPriorityIndicator>(string identity)
		where TPriorityIndicator : class, IPriorityIndicator;

	DistributedWorkersPrioritizationBuilder AddIdentityPriorityIndicator(string identity, Func<IServiceProvider, IPriorityIndicator> implementationFactory);
}