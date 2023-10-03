namespace RecurrentWorkerService.Distributed.Interfaces.Persistence.Models;

public struct NodePriorityEvent
{
	public long Revision { get; set; }
	
	public long NodeId { get; set; }

	public byte? Priority { get; set; }
}