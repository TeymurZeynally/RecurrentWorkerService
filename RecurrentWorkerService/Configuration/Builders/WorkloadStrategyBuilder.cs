using RecurrentWorkerService.Schedules.WorkloadScheduleModels;

namespace RecurrentWorkerService.Configuration.Builders;

public class WorkloadStrategyBuilder
{
	internal WorkloadStrategyBuilder()
	{
	}

	public WorkloadStrategyBuilder Multiply(byte workload, double value)
	{
		_workloadStrategies.Add(new WorkloadStrategy { Action = StrategyAction.Multiply, Workload = workload, ActionCoefficient = value });
		return this;
	}

	public WorkloadStrategyBuilder Divide(byte workload, double value)
	{
		_workloadStrategies.Add(new WorkloadStrategy
			{ Action = StrategyAction.Divide, Workload = workload, ActionCoefficient = value });
		return this;
	}

	public WorkloadStrategyBuilder Add(byte workload, TimeSpan value)
	{
		_workloadStrategies.Add(new WorkloadStrategy { Action = StrategyAction.Add, Workload = workload, ActionPeriod = value });
		return this;
	}

	public WorkloadStrategyBuilder Subtract(byte workload, TimeSpan value)
	{
		_workloadStrategies.Add(new WorkloadStrategy { Action = StrategyAction.Subtract, Workload = workload, ActionPeriod = value });
		return this;
	}

	public WorkloadStrategyBuilder Set(byte workload, TimeSpan value)
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