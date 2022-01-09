using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Configuration.Builders;
using RecurrentWorkerService.Schedules;
using RecurrentWorkerService.Services;
using RecurrentWorkerService.Services.Calculators;
using RecurrentWorkerService.Workers;

namespace RecurrentWorkerService.Registration;

public static class HostedServiceExtensions
{
	public static IServiceCollection AddRecurrentWorker<TWorker>(
		this IServiceCollection services,
		Action<RecurrentScheduleBuilder> scheduleBuilderAction)
		where TWorker : class, IRecurrentWorker
	{
		var scheduleBuilder = new RecurrentScheduleBuilder(new RecurrentSchedule());
		scheduleBuilderAction(scheduleBuilder);

		services.AddTransient<TWorker>();
		services.TryAddSingleton<RecurrentWorkerExecutionDelayCalculator>();
		services.AddHostedService(s => new RecurrentWorkerHostedService(
			s.GetService<ILogger<RecurrentWorkerHostedService>>()!,
			() => s.GetService<TWorker>()!,
			scheduleBuilder.Build(),
			s.GetService<RecurrentWorkerExecutionDelayCalculator>()!));
		return services;
	}

	public static IServiceCollection AddRecurrentWorker(
		this IServiceCollection services,
		Func<IServiceProvider, IRecurrentWorker> implementationFactory,
		Action<RecurrentScheduleBuilder> scheduleBuilderAction)
	{
		var scheduleBuilder = new RecurrentScheduleBuilder(new RecurrentSchedule());
		scheduleBuilderAction(scheduleBuilder);

		services.TryAddSingleton<RecurrentWorkerExecutionDelayCalculator>();
		services.AddHostedService(s => new RecurrentWorkerHostedService(
			s.GetService<ILogger<RecurrentWorkerHostedService>>()!,
			() => implementationFactory(s),
			scheduleBuilder.Build(),
			s.GetService<RecurrentWorkerExecutionDelayCalculator>()!));
		return services;
	}

	public static IServiceCollection AddCronWorker<TWorker>(
		this IServiceCollection services,
		Action<CronScheduleBuilder> scheduleBuilderAction)
		where TWorker : class, ICronWorker
	{
		var scheduleBuilder = new CronScheduleBuilder(new CronSchedule());
		scheduleBuilderAction(scheduleBuilder);

		services.AddTransient<TWorker>();
		services.TryAddSingleton<CronWorkerExecutionDelayCalculator>();
		services.AddHostedService(s => new CronWorkerHostedService(
			s.GetService<ILogger<CronWorkerHostedService>>()!,
			() => s.GetService<TWorker>()!,
			scheduleBuilder.Build(),
			s.GetService<CronWorkerExecutionDelayCalculator>()!));
		return services;
	}

	public static IServiceCollection AddCronWorker(
		this IServiceCollection services,
		Func<IServiceProvider, ICronWorker> implementationFactory,
		Action<CronScheduleBuilder> scheduleBuilderAction)
	{
		var scheduleBuilder = new CronScheduleBuilder(new CronSchedule());
		scheduleBuilderAction(scheduleBuilder);

		services.TryAddSingleton<CronWorkerExecutionDelayCalculator>();
		services.AddHostedService(s => new CronWorkerHostedService(
			s.GetService<ILogger<CronWorkerHostedService>>()!,
			() => implementationFactory(s),
			scheduleBuilder.Build(),
			s.GetService<CronWorkerExecutionDelayCalculator>()!));
		return services;
	}

	public static IServiceCollection AddWorkloadWorker<TWorker>(
		this IServiceCollection services,
		Action<WorkloadScheduleBuilder> scheduleBuilderAction)
		where TWorker : class, IWorkloadWorker
	{
		var scheduleBuilder = new WorkloadScheduleBuilder(new WorkloadSchedule());
		scheduleBuilderAction(scheduleBuilder);

		services.AddTransient<TWorker>();
		services.TryAddSingleton<WorkloadWorkerExecutionDelayCalculator>();
		services.AddHostedService(s => new WorkloadWorkerHostedService(
			s.GetService<ILogger<WorkloadWorkerHostedService>>()!,
			() => s.GetService<TWorker>()!,
			scheduleBuilder.Build(),
			s.GetService<WorkloadWorkerExecutionDelayCalculator>()!));
		return services;
	}

	public static IServiceCollection AddWorkloadWorker(
		this IServiceCollection services,
		Func<IServiceProvider, IWorkloadWorker> implementationFactory,
		Action<WorkloadScheduleBuilder> scheduleBuilderAction)
	{
		var scheduleBuilder = new WorkloadScheduleBuilder(new WorkloadSchedule());
		scheduleBuilderAction(scheduleBuilder);

		services.TryAddSingleton<WorkloadWorkerExecutionDelayCalculator>();
		services.AddHostedService(s => new WorkloadWorkerHostedService(
			s.GetService<ILogger<WorkloadWorkerHostedService>>()!,
			() => implementationFactory(s),
			scheduleBuilder.Build(),
			s.GetService<WorkloadWorkerExecutionDelayCalculator>()!));
		return services;
	}
}