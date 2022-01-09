using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Configuration.Builders;
using RecurrentWorkerService.Distributed.Persistence;
using RecurrentWorkerService.Distributed.Schedules;
using RecurrentWorkerService.Distributed.Services;
using RecurrentWorkerService.Distributed.Services.Calculators;
using RecurrentWorkerService.Workers;

namespace RecurrentWorkerService.Distributed.Registration;

public class DistributedWorkersRegistrationExtensions
{
	private readonly IServiceCollection _services;

	public DistributedWorkersRegistrationExtensions(IServiceCollection services)
	{
		_services = services;
		_services.AddSingleton<IExecutionDateCalculator, ExecutionDateCalculator>();
		_services.AddHostedService<HeartbeatService>();
	}

	public DistributedWorkersRegistrationExtensions AddDistributedRecurrentWorker<TWorker>(
		string identity,
		Action<DistributedRecurrentScheduleBuilder> scheduleBuilderAction)
		where TWorker : class, IRecurrentWorker
	{
		var scheduleBuilder = new DistributedRecurrentScheduleBuilder(new DistributedRecurrentSchedule());
		scheduleBuilderAction(scheduleBuilder);

		_services.AddTransient<TWorker>();
		_services.AddHostedService(s => new DistributedRecurrentWorkerHostedService(
			s.GetService<ILogger<DistributedRecurrentWorkerHostedService>>()!,
			() => s.GetService<TWorker>()!,
			scheduleBuilder.Build(),
			s.GetService<IExecutionDateCalculator>()!,
			s.GetService<IPersistence>()!,
			identity));
		return this;
	}

	public DistributedWorkersRegistrationExtensions AddDistributedRecurrentWorker(
		string identity,
		Func<IServiceProvider, IRecurrentWorker> implementationFactory,
		Action<DistributedRecurrentScheduleBuilder> scheduleBuilderAction)
	{
		var scheduleBuilder = new DistributedRecurrentScheduleBuilder(new DistributedRecurrentSchedule());
		scheduleBuilderAction(scheduleBuilder);

		_services.AddHostedService(s => new DistributedRecurrentWorkerHostedService(
			s.GetService<ILogger<DistributedRecurrentWorkerHostedService>>()!,
			() => implementationFactory(s),
			scheduleBuilder.Build(),
			s.GetService<IExecutionDateCalculator>()!,
			s.GetService<IPersistence>()!,
			identity));
		return this;
	}
}