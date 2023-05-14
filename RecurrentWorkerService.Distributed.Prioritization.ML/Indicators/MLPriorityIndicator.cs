using RecurrentWorkerService.Distributed.Prioritization.Indicators;
using RecurrentWorkerService.Distributed.Prioritization.ML.Indicators.ML;
using RecurrentWorkerService.Distributed.Prioritization.ML.Indicators.Models;
using RecurrentWorkerService.Distributed.Prioritization.ML.Repository;

namespace RecurrentWorkerService.Distributed.Prioritization.ML.Indicators
{
	internal class MLPriorityIndicator : IPriorityIndicator
	{
		private readonly MetricsInfluence _influence;
		private readonly PriorityModel _model;
		private readonly IMetricsRepository _repository;

		public MLPriorityIndicator(MetricsInfluence influence, PriorityModel model, IMetricsRepository repository)
		{
			_influence = influence;
			_model = model;
			_repository = repository;
		}

		public byte GetMeasurement()
		{
			var metricsData = _repository.GetLast(10);

			if (metricsData.Length < 10)
			{
				return byte.MinValue;
			}

			return (byte)(byte.MaxValue * _model.Predict(metricsData, _influence));
		}
	}
}
