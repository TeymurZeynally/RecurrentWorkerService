namespace RecurrentWorkerService.Schedules.WorkloadScheduleModels;

internal class WorkloadStrategy
{
	public byte Workload { get; set; }

	public StrategyAction Action { get; set; }

	public double ActionCoefficient { get; set; }

	public TimeSpan ActionPeriod { get; set; }
}