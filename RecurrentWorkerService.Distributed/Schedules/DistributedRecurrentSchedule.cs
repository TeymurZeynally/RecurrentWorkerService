using RecurrentWorkerService.Schedules;

namespace RecurrentWorkerService.Distributed.Schedules;

internal class DistributedRecurrentSchedule : RecurrentSchedule
{
	public int ExecutionCount { get; set; }
}