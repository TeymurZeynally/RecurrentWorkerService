using Microsoft.ML.Data;

namespace RecurrentWorkerService.Distributed.Prioritization.ML.Indicators.ML.Inputs
{
	internal class PriorityModelInput
	{
		[ColumnName("input_1")]
		[VectorType(1, 10, 3)]
		public float[] TimeSeries { get; set; }

		[ColumnName("input_2")]
		[VectorType(0, 3)]
		public float[] Weights { get; set; }
	}
}
