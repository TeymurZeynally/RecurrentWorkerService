namespace RecurrentWorkerService.Distributed.Interfaces.Persistence.Models;

public class WorkloadInfo
{
	public DateTimeOffset ExecutionDate { get; set; }

	public byte Workload { get; set; }

	public TimeSpan Elapsed { get; set; }
		
	public TimeSpan LastDelay { get; set; }

	public bool IsError { get; set; }
}