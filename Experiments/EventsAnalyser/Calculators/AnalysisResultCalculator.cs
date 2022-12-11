using EventsAnalyser.Calculators.Models;
using MathNet.Numerics.Statistics;

namespace EventsAnalyser.Calculators;

internal static class AnalysisResultCalculator
{
	public static AnalysisResult Calculate(string name, (double Value, string Id)[] measurements)
	{
		checked
		{
			var values = measurements.Select(x => x.Value).ToArray();

			var threeSigma = values.StandardDeviation() * 3;
			var mean = values.Mean();
			var max = values.Max();
			var grossErrors = measurements.Where(x => x.Value > mean + threeSigma || x.Value < mean - threeSigma)
				.ToArray();
			var grossErrorsValues = grossErrors.Select(x => x.Value).ToHashSet();

			var errorlessValues = values.Where(x => !grossErrorsValues.Contains(x)).ToArray();
			var variance = errorlessValues.Variance();
			var statisticalError = Math.Sqrt(variance / errorlessValues.Length);

			return new AnalysisResult
			{
				Parameter = name,
				Count = values.Length,
				Max = max,
				Mean = mean,
				MeanErrorless = errorlessValues.Mean(),
				Variance = variance,
				StandardDeviation = errorlessValues.StandardDeviation(),
				Error = statisticalError,
				Errors = grossErrors,
			};
		}
	}
}