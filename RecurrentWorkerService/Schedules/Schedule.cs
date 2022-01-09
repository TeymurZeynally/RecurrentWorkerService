namespace RecurrentWorkerService.Schedules;

internal abstract class Schedule
{
	public TimeSpan? RetryOnFailDelay { get; set; }
}