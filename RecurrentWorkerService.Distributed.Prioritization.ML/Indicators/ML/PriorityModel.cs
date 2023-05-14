using Microsoft.ML;
using RecurrentWorkerService.Distributed.Prioritization.ML.Indicators.ML.Inputs;
using RecurrentWorkerService.Distributed.Prioritization.ML.Indicators.ML.Outputs;
using RecurrentWorkerService.Distributed.Prioritization.ML.Indicators.Models;
using RecurrentWorkerService.Distributed.Prioritization.ML.Repository.Models;

namespace RecurrentWorkerService.Distributed.Prioritization.ML.Indicators.ML
{
	internal class PriorityModel
	{
		private readonly string _modelsDirectory;

		public PriorityModel(string modelsDirectory)
		{
			_modelsDirectory = modelsDirectory;
		}

		public float Predict(Metrics[] metrics, MetricsInfluence metricsInfluence)
		{
			var directory = new DirectoryInfo(_modelsDirectory);
			var lastModel = directory.GetFiles("*.onnx").MaxBy(x => x.LastWriteTimeUtc); 
			if(lastModel == null){ return 0f; }
			
			var mlContext = new MLContext();
			var predictionPipeline = mlContext.Transforms.ApplyOnnxModel(new[] { "lambda" }, new[] { "input_1", "input_2" }, lastModel.FullName);
			var emptyDv = mlContext.Data.LoadFromEnumerable(Array.Empty<PriorityModelInput>());
			var transformer = predictionPipeline.Fit(emptyDv);
			var engine = mlContext.Model.CreatePredictionEngine<PriorityModelInput, PriorityModelOutput>(transformer);

			var input = new PriorityModelInput
			{
				TimeSeries = metrics.SelectMany(x => new[] { x.Cpu, x.Memory, x.Network }).ToArray(),
				Weights = new[] { metricsInfluence.Cpu, metricsInfluence.Memory, metricsInfluence.Network }
			};

			var result = engine.Predict(input);

			return result.Priority.FirstOrDefault();
		}
	}
}
