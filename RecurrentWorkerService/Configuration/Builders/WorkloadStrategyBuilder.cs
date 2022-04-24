using RecurrentWorkerService.Schedules.WorkloadScheduleModels;
using RecurrentWorkerService.Workers.Models;

namespace RecurrentWorkerService.Configuration.Builders;

public class WorkloadStrategyBuilder
{
	internal WorkloadStrategyBuilder()
	{
	}

	public WorkloadStrategyBuilder Multiply(Workload workload, double value)
	{
		_workloadStrategies.Add(new WorkloadStrategy { Action = StrategyAction.Multiply, Workload = workload, ActionCoefficient = value });
		return this;
	}

	public WorkloadStrategyBuilder Divide(Workload workload, double value)
	{
		_workloadStrategies.Add(new WorkloadStrategy
			{ Action = StrategyAction.Divide, Workload = workload, ActionCoefficient = value });
		return this;
	}

	public WorkloadStrategyBuilder Add(Workload workload, TimeSpan value)
	{
		_workloadStrategies.Add(new WorkloadStrategy { Action = StrategyAction.Add, Workload = workload, ActionPeriod = value });
		return this;
	}

	public WorkloadStrategyBuilder Subtract(Workload workload, TimeSpan value)
	{
		_workloadStrategies.Add(new WorkloadStrategy { Action = StrategyAction.Subtract, Workload = workload, ActionPeriod = value });
		return this;
	}

	public WorkloadStrategyBuilder Set(Workload workload, TimeSpan value)
	{
		_workloadStrategies.Add(new WorkloadStrategy { Action = StrategyAction.Set, Workload = workload, ActionPeriod = value });
		return this;
	}

	internal WorkloadStrategy[] Build()
	{
		return _workloadStrategies.ToArray();
	}

	private readonly List<WorkloadStrategy> _workloadStrategies = new List<WorkloadStrategy>();
}