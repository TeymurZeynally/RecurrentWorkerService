namespace RecurrentWorkerService.Distributed.Prioritization.ML.Services.Models
{
	internal class MetricCollectionSettings
	{
		public required TimeSpan CollectionInterval { get; init; }

		public required TimeSpan ForecastInterval { get; init; }

		public required TimeSpan ModelTrainInterval { get; init; }

		public required string ModelsDirectory { get; init; }
	}
}
