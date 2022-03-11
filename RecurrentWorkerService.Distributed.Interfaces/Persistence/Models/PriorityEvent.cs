namespace RecurrentWorkerService.Distributed.Interfaces.Persistence.Models
{
	public struct PriorityEvent
	{
		public string Identity { get; set; }

		public long NodeId { get; set; }

		public byte? Priority { get; set; }
	}
}
