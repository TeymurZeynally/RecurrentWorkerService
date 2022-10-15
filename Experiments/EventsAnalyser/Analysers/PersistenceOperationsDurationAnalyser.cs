using EventsAnalyser.Calculators;
using EventsAnalyser.Calculators.Models;
using EventsAnalyser.Queries;
using EventsAnalyser.Queries.Models;
using InfluxDB.Client;

namespace EventsAnalyser.Analysers
{
	internal class PersistenceOperationsDurationAnalyser
	{
		private readonly QueryApi _queryApi;

		public PersistenceOperationsDurationAnalyser(QueryApi queryApi)
		{
			_queryApi = queryApi;
		}

		public async Task<AnalysisResult[]> Analyse()
		{
			var query = await File.ReadAllTextAsync("Queries/QueryPersistenceOperationsDuration.txt").ConfigureAwait(false);

			Console.WriteLine(query);

			var operations = await _queryApi.QueryAsync<PersistenceOperation>(query, "TZ").ConfigureAwait(false);

			return operations
				.GroupBy(x => x.Name)
				.Select(x => AnalysisResultCalculator.Calculate(x.Key, x.Select(v => ((double)v.DurationNanoseconds, v.TraceId)).ToArray()))
				.ToArray();
		}
	}
}
