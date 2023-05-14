namespace RecurrentWorkerService.Distributed.Prioritization.ML.Indicators.Models
{
	internal class MetricsInfluence
	{
		public required float Cpu { get; init; }

		public required float Memory { get; init; }

		public required float Network { get; init; }
	}
}
