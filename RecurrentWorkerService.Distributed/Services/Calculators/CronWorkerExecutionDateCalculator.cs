using NCrontab;

namespace RecurrentWorkerService.Distributed.Services.Calculators;

internal class CronWorkerExecutionDateCalculator
{
	public (long N, DateTimeOffset ExecutionDate) CalculateNextExecutionDate(string cronExpression, DateTimeOffset now)
	{
		var nextExecutionDate = (DateTimeOffset)CrontabSchedule.Parse(cronExpression).GetNextOccurrence(now.UtcDateTime);

		return (nextExecutionDate.UtcTicks, nextExecutionDate);
	}
}