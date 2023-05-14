using Microsoft.ML.Data;

namespace RecurrentWorkerService.Distributed.Prioritization.ML.Indicators.ML.Outputs
{
	internal class PriorityModelOutput
	{
		[ColumnName("lambda")]
		[VectorType(1, 1)]
		public float[] Priority { get; set; }
	}
}
