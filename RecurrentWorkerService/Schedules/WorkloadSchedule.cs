using RecurrentWorkerService.Schedules.WorkloadScheduleModels;

namespace RecurrentWorkerService.Schedules;

internal class WorkloadSchedule : Schedule
{
	public TimeSpan PeriodFrom { get; set; }

	public TimeSpan PeriodTo { get; set; }

	public WorkloadStrategy[] Strategies { get; set; } = Array.Empty<WorkloadStrategy>();
}