namespace RecurrentWorkerService.Distributed.Prioritization.Indicators
{
	internal class IdentityPriorityIndicator
	{
		public IdentityPriorityIndicator(IPriorityIndicator priorityIndicator, string identity)
		{
			_priorityIndicator = priorityIndicator;
			Identity = identity;
		}

		public string Identity { get; }

		public byte GetMeasurement() => _priorityIndicator.GetMeasurement();
		
		private readonly IPriorityIndicator _priorityIndicator;
	}
}
