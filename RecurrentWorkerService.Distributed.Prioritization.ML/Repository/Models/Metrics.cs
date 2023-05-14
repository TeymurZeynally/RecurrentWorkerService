namespace RecurrentWorkerService.Distributed.Prioritization.ML.Repository.Models
{
	internal class Metrics
	{
		public required float Cpu { get; init; }

		public required float Memory { get; init; }

		public required float Network { get; init; }
	}
}
