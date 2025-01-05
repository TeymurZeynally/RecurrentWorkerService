using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Configuration.Builders;
using RecurrentWorkerService.Distributed.Interfaces.Persistence;
using RecurrentWorkerService.Distributed.Interfaces.Prioritization;
using RecurrentWorkerService.Distributed.Services;
using RecurrentWorkerService.Distributed.Services.Calculators;
using RecurrentWorkerService.Distributed.Services.Hosts;
using RecurrentWorkerService.Schedules;
using RecurrentWorkerService.Services.Calculators;
using RecurrentWorkerService.Workers;

namespace RecurrentWorkerService.Distributed.Registration;

public class DistributedWorkersRegistrationBuilder
{
	private readonly ActivitySource _activitySource;
	private readonly IServiceCollection _services;
	private readonly long _nodeId;

	public DistributedWorkersRegistrationBuilder(IServiceCollection services, long nodeId)
	{
		var assembly = System.Reflection.Assembly.GetExecutingAssembly().GetName();

		_activitySource = new ActivitySource(assembly.Name!, assembly.Version?.ToString());
		_services = services;
		_nodeId = nodeId;

		_services.AddSingleton<RecurrentWorkerExecutionDateCalculator>();
		_services.AddSingleton<CronWorkerExecutionDateCalculator>();
		_services.AddSingleton<WorkloadWorkerExecutionDelayCalculator>();

		_services.AddHostedService<HeartbeatHostService>();
	}

	public DistributedWorkersRegistrationBuilder AddDistributedRecurrentWorker<TWorker>(
		string identity,
		Action<RecurrentScheduleBuilder> scheduleBuilderAction)
		where TWorker : class, IRecurrentWorker
	{
		var scheduleBuilder = new RecurrentScheduleBuilder(new RecurrentSchedule());
		scheduleBuilderAction(scheduleBuilder);

		_services.AddTransient<TWorker>();
		_services.AddTransient<IDistributedWorkerService>(s => new DistributedRecurrentWorkerService(
			s.GetRequiredService<ILogger<DistributedRecurrentWorkerService>>(),
			s.GetRequiredService<TWorker>,
			scheduleBuilder.Build(),
			s.GetRequiredService<RecurrentWorkerExecutionDateCalculator>(),
			s.GetRequiredService<IPersistence>(),
			s.GetRequiredService<IPriorityManager>(),
			identity,
			_nodeId,
			_activitySource));
		return this;
	}

	public DistributedWorkersRegistrationBuilder AddDistributedRecurrentWorker(
		string identity,
		Func<IServiceProvider, IRecurrentWorker> implementationFactory,
		Action<RecurrentScheduleBuilder> scheduleBuilderAction)
	{
		var scheduleBuilder = new RecurrentScheduleBuilder(new RecurrentSchedule());
		scheduleBuilderAction(scheduleBuilder);

		_services.AddTransient<IDistributedWorkerService>(s => new DistributedRecurrentWorkerService(
			s.GetRequiredService<ILogger<DistributedRecurrentWorkerService>>(),
			() => implementationFactory(s),
			scheduleBuilder.Build(),
			s.GetRequiredService<RecurrentWorkerExecutionDateCalculator>(),
			s.GetRequiredService<IPersistence>(),
			s.GetRequiredService<IPriorityManager>(),
			identity,
			_nodeId,
			_activitySource));
		return this;
	}

	public DistributedWorkersRegistrationBuilder AddDistributedRecurrentWorker<TWorker>(
		string identity,
		Action<RecurrentScheduleBuilder> scheduleBuilderAction,
		Action<DistributedMultiiterationWorkerSettingsBuilder> multiiterationWorkerSettingsBuilderAction)
	where TWorker : class, IRecurrentWorker
	{
		var scheduleBuilder = new RecurrentScheduleBuilder(new RecurrentSchedule());
		scheduleBuilderAction(scheduleBuilder);

		var multiiterationWorkerSettingsBuilder = new DistributedMultiiterationWorkerSettingsBuilder();
		multiiterationWorkerSettingsBuilderAction(multiiterationWorkerSettingsBuilder);

		_services.AddTransient<TWorker>();
		_services.AddTransient<IDistributedWorkerService>(s => new DistributedRecurrentMultipleIterationWorkerService(
			s.GetRequiredService<ILogger<DistributedRecurrentMultipleIterationWorkerService>>(),
			s.GetRequiredService<TWorker>,
			scheduleBuilder.Build(),
			s.GetRequiredService<RecurrentWorkerExecutionDateCalculator>(),
			s.GetRequiredService<IPersistence>(),
			s.GetRequiredService<IPriorityManager>(),
			identity,
			multiiterationWorkerSettingsBuilder.Build().MultiIterationOnNodeMaxDuration));
		return this;
	}

	public DistributedWorkersRegistrationBuilder AddDistributedRecurrentWorker(
		string identity,
		Func<IServiceProvider, IRecurrentWorker> implementationFactory,
		Action<RecurrentScheduleBuilder> scheduleBuilderAction,
		Action<DistributedMultiiterationWorkerSettingsBuilder> multiiterationWorkerSettingsBuilderAction)
	{
		var scheduleBuilder = new RecurrentScheduleBuilder(new RecurrentSchedule());
		scheduleBuilderAction(scheduleBuilder);

		var multiiterationWorkerSettingsBuilder = new DistributedMultiiterationWorkerSettingsBuilder();
		multiiterationWorkerSettingsBuilderAction(multiiterationWorkerSettingsBuilder);

		_services.AddTransient<IDistributedWorkerService>(s => new DistributedRecurrentMultipleIterationWorkerService(
			s.GetRequiredService<ILogger<DistributedRecurrentMultipleIterationWorkerService>>(),
			() => implementationFactory(s),
			scheduleBuilder.Build(),
			s.GetRequiredService<RecurrentWorkerExecutionDateCalculator>(),
			s.GetRequiredService<IPersistence>(),
			s.GetRequiredService<IPriorityManager>(),
			identity,
			multiiterationWorkerSettingsBuilder.Build().MultiIterationOnNodeMaxDuration));
		return this;
	}

	public DistributedWorkersRegistrationBuilder AddDistributedCronWorker<TWorker>(
		string identity,
		Action<CronScheduleBuilder> scheduleBuilderAction)
		where TWorker : class, ICronWorker
	{
		var scheduleBuilder = new CronScheduleBuilder(new CronSchedule());
		scheduleBuilderAction(scheduleBuilder);

		_services.AddTransient<TWorker>();
		_services.AddTransient<IDistributedWorkerService>(s => new DistributedCronWorkerService(
			s.GetRequiredService<ILogger<DistributedCronWorkerService>>(),
			s.GetRequiredService<TWorker>,
			scheduleBuilder.Build(),
			s.GetRequiredService<CronWorkerExecutionDateCalculator>(),
			s.GetRequiredService<IPersistence>(),
			s.GetRequiredService<IPriorityManager>(),
			identity,
			_nodeId,
			_activitySource));
		return this;
	}

	public DistributedWorkersRegistrationBuilder AddDistributedCronWorker(
		string identity,
		Func<IServiceProvider, ICronWorker> implementationFactory,
		Action<CronScheduleBuilder> scheduleBuilderAction)
	{
		var scheduleBuilder = new CronScheduleBuilder(new CronSchedule());
		scheduleBuilderAction(scheduleBuilder);

		_services.AddTransient<IDistributedWorkerService>(s => new DistributedCronWorkerService(
			s.GetRequiredService<ILogger<DistributedCronWorkerService>>(),
			() => implementationFactory(s),
			scheduleBuilder.Build(),
			s.GetRequiredService<CronWorkerExecutionDateCalculator>(),
			s.GetRequiredService<IPersistence>(),
			s.GetRequiredService<IPriorityManager>(),
			identity,
			_nodeId,
			_activitySource));
		return this;
	}


	public DistributedWorkersRegistrationBuilder AddDistributedCronWorker<TWorker>(
		string identity,
		Action<CronScheduleBuilder> scheduleBuilderAction,
		Action<DistributedMultiiterationWorkerSettingsBuilder> multiiterationWorkerSettingsBuilderAction)
		where TWorker : class, ICronWorker
	{
		var scheduleBuilder = new CronScheduleBuilder(new CronSchedule());
		scheduleBuilderAction(scheduleBuilder);

		var multiiterationWorkerSettingsBuilder = new DistributedMultiiterationWorkerSettingsBuilder();
		multiiterationWorkerSettingsBuilderAction(multiiterationWorkerSettingsBuilder);

		_services.AddTransient<TWorker>();
		_services.AddTransient<IDistributedWorkerService>(s => new DistributedCronMultipleIterationWorkerService(
			s.GetRequiredService<ILogger<DistributedCronMultipleIterationWorkerService>>(),
			s.GetRequiredService<TWorker>,
			scheduleBuilder.Build(),
			s.GetRequiredService<CronWorkerExecutionDateCalculator>(),
			s.GetRequiredService<IPersistence>(),
			s.GetRequiredService<IPriorityManager>(),
			identity,
			multiiterationWorkerSettingsBuilder.Build().MultiIterationOnNodeMaxDuration));
		return this;
	}

	public DistributedWorkersRegistrationBuilder AddDistributedCronWorker(
		string identity,
		Func<IServiceProvider, ICronWorker> implementationFactory,
		Action<CronScheduleBuilder> scheduleBuilderAction,
		Action<DistributedMultiiterationWorkerSettingsBuilder> multiiterationWorkerSettingsBuilderAction)
	{
		var scheduleBuilder = new CronScheduleBuilder(new CronSchedule());
		scheduleBuilderAction(scheduleBuilder);

		var multiiterationWorkerSettingsBuilder = new DistributedMultiiterationWorkerSettingsBuilder();
		multiiterationWorkerSettingsBuilderAction(multiiterationWorkerSettingsBuilder);

		_services.AddTransient<IDistributedWorkerService>(s => new DistributedCronMultipleIterationWorkerService(
			s.GetRequiredService<ILogger<DistributedCronMultipleIterationWorkerService>>(),
			() => implementationFactory(s),
			scheduleBuilder.Build(),
			s.GetRequiredService<CronWorkerExecutionDateCalculator>(),
			s.GetRequiredService<IPersistence>(),
			s.GetRequiredService<IPriorityManager>(),
			identity,
			multiiterationWorkerSettingsBuilder.Build().MultiIterationOnNodeMaxDuration));
		return this;
	}

	public DistributedWorkersRegistrationBuilder AddDistributedWorkloadWorker<TWorker>(
		string identity,
		Action<WorkloadScheduleBuilder> scheduleBuilderAction)
		where TWorker : class, IWorkloadWorker
	{
		var scheduleBuilder = new WorkloadScheduleBuilder(new WorkloadSchedule());
		scheduleBuilderAction(scheduleBuilder);

		_services.AddTransient<TWorker>();
		_services.AddTransient<IDistributedWorkerService>(s => new DistributedWorkloadWorkerService(
			s.GetRequiredService<ILogger<DistributedWorkloadWorkerService>>(),
			s.GetRequiredService<TWorker>,
			scheduleBuilder.Build(),
			s.GetRequiredService<WorkloadWorkerExecutionDelayCalculator>(),
			s.GetRequiredService<IPersistence>(),
			s.GetRequiredService<IPriorityManager>(),
			identity,
			_nodeId,
			_activitySource));
		return this;
	}

	public DistributedWorkersRegistrationBuilder AddDistributedWorkloadWorker(
		string identity,
		Func<IServiceProvider, IWorkloadWorker> implementationFactory,
		Action<WorkloadScheduleBuilder> scheduleBuilderAction)
	{
		var scheduleBuilder = new WorkloadScheduleBuilder(new WorkloadSchedule());
		scheduleBuilderAction(scheduleBuilder);

		_services.AddTransient<IDistributedWorkerService>(s => new DistributedWorkloadWorkerService(
			s.GetRequiredService<ILogger<DistributedWorkloadWorkerService>>(),
			() => implementationFactory(s),
			scheduleBuilder.Build(),
			s.GetRequiredService<WorkloadWorkerExecutionDelayCalculator>(),
			s.GetRequiredService<IPersistence>(),
			s.GetRequiredService<IPriorityManager>(),
			identity,
			_nodeId,
			_activitySource));
		return this;
	}

	public DistributedWorkersRegistrationBuilder AddDistributedWorkloadWorker<TWorker>(
		string identity,
		Action<WorkloadScheduleBuilder> scheduleBuilderAction,
		Action<DistributedMultiiterationWorkerSettingsBuilder> multiiterationWorkerSettingsBuilderAction)
		where TWorker : class, IWorkloadWorker
	{
		var scheduleBuilder = new WorkloadScheduleBuilder(new WorkloadSchedule());
		scheduleBuilderAction(scheduleBuilder);

		var multiiterationWorkerSettingsBuilder = new DistributedMultiiterationWorkerSettingsBuilder();
		multiiterationWorkerSettingsBuilderAction(multiiterationWorkerSettingsBuilder);

		_services.AddTransient<TWorker>();
		_services.AddTransient<IDistributedWorkerService>(s => new DistributedWorkloadMultipleIterationWorkerService(
			s.GetRequiredService<ILogger<DistributedWorkloadMultipleIterationWorkerService>>(),
			s.GetRequiredService<TWorker>,
			scheduleBuilder.Build(),
			s.GetRequiredService<WorkloadWorkerExecutionDelayCalculator>(),
			s.GetRequiredService<IPersistence>(),
			s.GetRequiredService<IPriorityManager>(),
			identity,
			multiiterationWorkerSettingsBuilder.Build().MultiIterationOnNodeMaxDuration));
		return this;
	}

	public DistributedWorkersRegistrationBuilder AddDistributedWorkloadWorker(
		string identity,
		Func<IServiceProvider, IWorkloadWorker> implementationFactory,
		Action<WorkloadScheduleBuilder> scheduleBuilderAction,
		Action<DistributedMultiiterationWorkerSettingsBuilder> multiiterationWorkerSettingsBuilderAction)
	{
		var scheduleBuilder = new WorkloadScheduleBuilder(new WorkloadSchedule());
		scheduleBuilderAction(scheduleBuilder);

		var multiiterationWorkerSettingsBuilder = new DistributedMultiiterationWorkerSettingsBuilder();
		multiiterationWorkerSettingsBuilderAction(multiiterationWorkerSettingsBuilder);

		_services.AddTransient<IDistributedWorkerService>(s => new DistributedWorkloadMultipleIterationWorkerService(
			s.GetRequiredService<ILogger<DistributedWorkloadMultipleIterationWorkerService>>(),
			() => implementationFactory(s),
			scheduleBuilder.Build(),
			s.GetRequiredService<WorkloadWorkerExecutionDelayCalculator>(),
			s.GetRequiredService<IPersistence>(),
			s.GetRequiredService<IPriorityManager>(),
			identity,
			multiiterationWorkerSettingsBuilder.Build().MultiIterationOnNodeMaxDuration));
		return this;
	}

}