namespace RecurrentWorkerService.Schedules;

internal class CronSchedule : Schedule
{
	public string CronExpression { get; set; } = "* * * * *";
}