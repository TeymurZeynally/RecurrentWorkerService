using EventsAnalyser.Builders;
using EventsAnalyser.Queries;
using EventsAnalyser.Queries.Models;
using InfluxDB.Client;

namespace EventsAnalyser.Analysers
{
	internal class LibAndCodeOperationsAnalyser
	{
		private readonly QueryApi _queryApi;

		public LibAndCodeOperationsAnalyser(QueryApi queryApi)
		{
			_queryApi = queryApi;
		}

		public async Task<TimeSpan[]> Validate(string name, PayloadType payload, string identity)
		{
			var payloadName = $"{payload}Payload.ExecuteAsync";
			var parameters = QueryParametersBuilder.Build(new { name, payload = payloadName, identity });
			var query = parameters + await File.ReadAllTextAsync("Queries/QueryLibAndCodeOperationsDuration.txt").ConfigureAwait(false);

			Console.WriteLine(query);

			var results = new List<TimeSpan>();

			await foreach (var operation in _queryApi.QueryAsyncEnumerable<LibAndCodeOperationsDuration>(query, "TZ").ConfigureAwait(false))
			{
				results.Add(operation.LibDuration - operation.LockDuration - operation.CodeDuration);

				if (results.Last() > TimeSpan.FromSeconds(1))
				{
					Console.WriteLine(operation.TraceId);
				}

				Console.WriteLine($"{operation.CodeDuration} | {operation.LibDuration} | {operation.LockDuration} | {operation.LibDuration - operation.LockDuration - operation.CodeDuration}");
			}

			return results.ToArray();
		}
	}
}
