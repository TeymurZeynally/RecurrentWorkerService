using RecurrentWorkerService.Extensions;
using RecurrentWorkerService.Schedules;
using RecurrentWorkerService.Schedules.WorkloadScheduleModels;
using RecurrentWorkerService.Workers.Models;

namespace RecurrentWorkerService.Services.Calculators;

internal class WorkloadWorkerExecutionDelayCalculator
{
	public TimeSpan Calculate(WorkloadSchedule schedule, TimeSpan lastDelay, Workload workload, bool isError)
	{
		if (isError && schedule.RetryOnFailDelay != null)
		{
			return schedule.RetryOnFailDelay.Value;
		}

		var strategy = schedule.Strategies.OrderBy(x => x.Workload).LastOrDefault(x => x.Workload <= workload);
		if (strategy == null)
		{
			return workload == Workload.Zero ? schedule.PeriodTo : schedule.PeriodFrom;
		}

		var computedDelay = strategy.Action switch
		{
			StrategyAction.Add => lastDelay + strategy.ActionPeriod,
			StrategyAction.Subtract => lastDelay - strategy.ActionPeriod,
			StrategyAction.Multiply => lastDelay * strategy.ActionCoefficient,
			StrategyAction.Divide => lastDelay / strategy.ActionCoefficient,
			StrategyAction.Set => strategy.ActionPeriod,
			_ => throw new ArgumentOutOfRangeException()
		};

		if (computedDelay > schedule.PeriodTo) computedDelay = schedule.PeriodTo;
		if (computedDelay < schedule.PeriodFrom) computedDelay = schedule.PeriodFrom;

		return TimeSpanExtensions.Max(computedDelay, TimeSpan.Zero);
	}
}