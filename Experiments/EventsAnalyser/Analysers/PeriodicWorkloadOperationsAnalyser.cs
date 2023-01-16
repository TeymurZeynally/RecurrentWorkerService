using EventsAnalyser.Builders;
using EventsAnalyser.Calculators;
using EventsAnalyser.Calculators.Models;
using EventsAnalyser.Queries.Models;
using FluentAssertions;
using InfluxDB.Client;
using RecurrentWorkerService.Schedules;
using RecurrentWorkerService.Services.Calculators;

namespace EventsAnalyser.Analysers;

internal class PeriodicWorkloadOperationsAnalyser
{
	private readonly QueryApi _queryApi;

	public PeriodicWorkloadOperationsAnalyser(QueryApi queryApi)
	{
		_queryApi = queryApi;
	}

	public async Task<AnalysisResult> Analyse(Interval interval, WorkloadSchedule schedule, PayloadType payload)
	{
		if(payload == PayloadType.Immediate)
		{
			return null;
		}

		var name = $"WorkloadWorker.ExecuteAsync-{payload}Payload";
		var parameters = QueryParametersBuilder.Build(
			new
			{
				name,
				startTimeStamp = interval.StartTimeStamp.UtcDateTime.ToString("O"),
				endTimeStamp = interval.EndTimeStamp.UtcDateTime.ToString("O"),
			});
		var query = parameters + await File.ReadAllTextAsync("Queries/QueryWorkloadOperationsTimeAndDuration.txt").ConfigureAwait(false);

		Console.WriteLine(query);

		var calcualtor = new WorkloadWorkerExecutionDelayCalculator();

		var deltas = new List<TimeSpan>();

		var previous = default(WorkloadOperationDuration);
		var workloadDelay = TimeSpan.Zero;

		var data = await Cache.Cache.Get<WorkloadOperationDuration>(
			query,
			async () => await _queryApi.QueryAsync<WorkloadOperationDuration>(query, "TZ")).ConfigureAwait(false);

		foreach (var operation in data)
		{
			if (previous != null)
			{
				var expectedDate = calcualtor.Calculate(schedule, workloadDelay, previous.Workload, false);

				
			}

			previous = operation;
		}

		deltas.Count.Should().NotBe(0);

		return AnalysisResultCalculator.Calculate($"Workload-{payload}", deltas.Select(v => (v.TotalNanoseconds, string.Empty)).ToArray());
	}
}