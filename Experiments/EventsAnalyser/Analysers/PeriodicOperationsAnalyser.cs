using EventsAnalyser.Builders;
using EventsAnalyser.Queries;
using EventsAnalyser.Queries.Models;
using InfluxDB.Client;

namespace EventsAnalyser.Analysers
{
	internal class PeriodicOperationsAnalyser
	{
		private readonly QueryApi _queryApi;

		public PeriodicOperationsAnalyser(QueryApi queryApi)
		{
			_queryApi = queryApi;
		}

		public async Task<TimeSpan[]> Validate(TimeSpan period, PayloadType payload, string identity)
		{
			var name = $"{payload}Payload.ExecuteAsync";
			var parameters = QueryParametersBuilder.Build(new { name, identity });
			var query = parameters + Flux.QueryOperationsTimeAndDuration;

			Console.WriteLine(query);

			var previous = default(PeriodicOperationDuration);
			var deltas = new List<TimeSpan>();

			await foreach (var operation in _queryApi.QueryAsyncEnumerable<PeriodicOperationDuration>(query, "KSS").ConfigureAwait(false))
			{
				if (previous != null)
				{
					var expectedDate = previous.DateTimeOffset + (previous.Duration > period ? previous.Duration : period);

					deltas.Add(operation.DateTimeOffset - expectedDate);

					Console.WriteLine($"{previous.DateTimeOffset} | {previous.Duration} | {expectedDate} | {operation.DateTimeOffset} | {operation.Duration}  | {deltas.Last()}");
				}


				previous = operation;
			}

			return deltas.ToArray();
		}
	}
}
