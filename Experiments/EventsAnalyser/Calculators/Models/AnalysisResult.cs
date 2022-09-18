namespace EventsAnalyser.Calculators.Models
{
	internal class AnalysisResult
	{
		public string Parameter { get; init; }

		/// <summary>
		/// x
		/// </summary>
		public double Mean { get; init; }

		/// <summary>
		/// σ
		/// </summary>
		public double StandardDeviation { get; init; }

		/// <summary>
		/// σ^2
		/// </summary>
		public double Variance { get; init; }

		/// <summary>
		/// Δx
		/// </summary>
		public double Error { get; init; }

		public IReadOnlyCollection<(double Value, string TraceId)> Errors { get; init; }
	}
}
