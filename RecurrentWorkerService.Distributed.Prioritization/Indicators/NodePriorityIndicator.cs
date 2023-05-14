namespace RecurrentWorkerService.Distributed.Prioritization.Indicators
{
	internal class NodePriorityIndicator
	{
		public NodePriorityIndicator(IPriorityIndicator priorityIndicator)
		{
			_priorityIndicator = priorityIndicator;
		}

		public byte GetMeasurement() => _priorityIndicator.GetMeasurement();

		private readonly IPriorityIndicator _priorityIndicator;
	}
}
