using NCrontab;
using RecurrentWorkerService.Schedules;

namespace RecurrentWorkerService.Services.Calculators;

internal class CronWorkerExecutionDelayCalculator
{
	public TimeSpan Calculate(CronSchedule schedule, bool isError)
	{
		if (isError && schedule.RetryOnFailDelay != null)
		{
			return schedule.RetryOnFailDelay.Value;
		}

		var now = DateTime.UtcNow;
		return CrontabSchedule.Parse(schedule.CronExpression).GetNextOccurrence(now) - now;
	}
}