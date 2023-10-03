using System.Runtime.InteropServices;
using System.Text;
using EventsAnalyser.Analysers;
using EventsAnalyser.Calculators.Models;
using EventsAnalyser.Helpers;
using EventsAnalyser.Queries.Models;
using FluentAssertions;
using InfluxDB.Client;
using RecurrentWorkerService.Schedules;
using RecurrentWorkerService.Schedules.WorkloadScheduleModels;
using RecurrentWorkerService.Workers.Models;
using Interval = EventsAnalyser.Queries.Models.Interval;

var planExecutionsFile = @"D:\Users\Teymur\Desktop\Dashboard\ExperimentRuns.csv";

var lines = (await File.ReadAllLinesAsync(planExecutionsFile).ConfigureAwait(false))
	.Select(x => x.Split(","))
	.Select(x => (Interval: new Interval(){ StartTimeStamp = DateTimeOffset.Parse(x[0]), EndTimeStamp = DateTimeOffset.Parse(x[1]) }, Name: x[2]))
	.ToArray();

var options = new InfluxDBClientOptions.Builder()
	.Url("http://192.168.1.64:8086")
	.AuthenticateToken(@"doJ2H3JHovP2lKYSTUq4gY3PitkM47fu2l11NApIvqGdBT6h_99NhHxgCOIgTWzOX05vQRdbCN3Vaj3PsHouvg==")
	.TimeOut(TimeSpan.FromMinutes(10))
	.Build();
var influxDbClient = InfluxDBClientFactory.Create(options);

var queryApi = influxDbClient.GetQueryApi();


var immediateWorkloadSchedule = new WorkloadSchedule
{
	PeriodFrom = TimeSpan.FromTicks(1),
	PeriodTo = TimeSpan.FromTicks(1),
	Strategies = new[]
	{
		new WorkloadStrategy{ Workload = Workload.Zero, Action = StrategyAction.Add, ActionPeriod = TimeSpan.FromSeconds(1) },
		new WorkloadStrategy{ Workload = Workload.FromPercent(50), Action = StrategyAction.Subtract, ActionPeriod = TimeSpan.FromSeconds(1) },
	}
};

var normalWorkloadSchedule = new WorkloadSchedule
{
	PeriodFrom = TimeSpan.FromSeconds(1),
	PeriodTo = TimeSpan.FromSeconds(10),
	Strategies = new[]
	{
		new WorkloadStrategy{ Workload = Workload.Zero, Action = StrategyAction.Add, ActionPeriod = TimeSpan.FromSeconds(1) },
		new WorkloadStrategy{ Workload = Workload.FromPercent(50), Action = StrategyAction.Subtract, ActionPeriod = TimeSpan.FromSeconds(1) },
	}
};

var configurationOfWorkers = new[]
{
	(Id: "Cron-Immediate", Payload: PayloadType.Immediate, Period: TimeSpan.FromMinutes(1), WorkloadSchedule: default(WorkloadSchedule)),
	(Id: "Cron-Fast", Payload: PayloadType.Fast, Period: TimeSpan.FromMinutes(1), WorkloadSchedule: default(WorkloadSchedule)),
	(Id: "Cron-Slow", Payload: PayloadType.Slow, Period: TimeSpan.FromMinutes(1), WorkloadSchedule: default(WorkloadSchedule)),
	(Id: "Recurrent-Immediate", Payload: PayloadType.Immediate, Period: TimeSpan.FromTicks(1), WorkloadSchedule: default(WorkloadSchedule)),
	(Id: "Recurrent-Fast", Payload: PayloadType.Fast, Period: TimeSpan.FromSeconds(1), WorkloadSchedule: default(WorkloadSchedule)),
	(Id: "Recurrent-Slow", Payload: PayloadType.Slow, Period: TimeSpan.FromSeconds(1), WorkloadSchedule: default(WorkloadSchedule)),
	(Id: "Workload-Immediate", Payload: PayloadType.Immediate, Period: default(TimeSpan?), WorkloadSchedule: immediateWorkloadSchedule),
	(Id: "Workload-Fast", Payload: PayloadType.Fast, Period: default(TimeSpan?), WorkloadSchedule: normalWorkloadSchedule),
	(Id: "Workload-Slow", Payload: PayloadType.Slow, Period: default(TimeSpan?), WorkloadSchedule: normalWorkloadSchedule),
};


foreach (var line in lines)
{
	Console.WriteLine();
	Console.WriteLine(line.Name);
	Console.WriteLine();

	var excludeList = new string[] {
		"Test 2.7: Storage bandwidth 1kbps",
		"Test 2.8: Storage bandwidth 512bps",
		"Test 5.2: Storage delay 300ms",
		"Test 5.3: Storage delay 500ms",
		"Test 5.4: Storage delay 700ms",
		"Test 5.5: Storage delay 900ms",
		"Test 5.6: Storage delay 1100ms",
		"Test 8.3: Storage loss percent 50%",
		"Test 14.2: Storage corrupt percent 30%",
		"Test 14.3: Storage corrupt percent 50%"
	};

	if (excludeList.Contains(line.Name)) continue;
	
	var periodicOperationsReults = new List<AnalysisResult>();
	var libAndCodeOperationsReults = new List<AnalysisResult>();
	var periodicOperationsIntersectionResults = new List<(string Name, bool IsValid)>();

	foreach (var config in configurationOfWorkers)
	{
		var periodicOperationsIntersectionResult = await new PeriodicOperationsIntersectionAnalyser(queryApi).Analyse(line.Interval, config.Payload, config.Id).ConfigureAwait(false);
		var libAndCodeOperationsAnalyserResult = await new LibAndCodeOperationsAnalyser(queryApi).Analyse(line.Interval, "DistributedRecurrentWorkerService", config.Payload, config.Id).ConfigureAwait(false);

		if(config.Period != null)
		{
			var periodicOperationsResult = await new PeriodicOperationsAnalyser(queryApi).Analyse(line.Interval, config.Period.Value, config.Payload, config.Id).ConfigureAwait(false);
			periodicOperationsResult.Should().NotBeNull();
			periodicOperationsReults.Add(periodicOperationsResult);
		}

		if(config.WorkloadSchedule != null)
		{
			var periodicOperationsResult = await new PeriodicWorkloadOperationsAnalyser(queryApi).Analyse(line.Interval, config.WorkloadSchedule, config.Payload, config.Id).ConfigureAwait(false);
			periodicOperationsResult.Should().NotBeNull();
			periodicOperationsReults.Add(periodicOperationsResult);
		}

		periodicOperationsIntersectionResult.Should().BeTrue();
		periodicOperationsIntersectionResults.Add((config.Id, periodicOperationsIntersectionResult));
		libAndCodeOperationsReults.Add(libAndCodeOperationsAnalyserResult);
	}


	PrintOperationsIntersectionCsv(line.Name, "PeriodicOperationsIntersection", periodicOperationsIntersectionResults);

	PrintCsv(line.Name, "PeriodicOperations", periodicOperationsReults.ToArray());
	Console.WriteLine();
	PrintCsv(line.Name, "LibAndCodeOperations", libAndCodeOperationsReults.ToArray());
	Console.WriteLine();

	var persistenceOperationsDurationResults = await new PersistenceOperationsDurationAnalyser(queryApi).Analyse(line.Interval).ConfigureAwait(false);
	PrintCsv(line.Name, "PersistenceOperationsDuration", persistenceOperationsDurationResults);
	Console.WriteLine();

	var prioritiesReceiveTimestampResult = await new PrioritiesReceiveTimestampAnalyser(queryApi).Analyse(line.Interval, "UpdatePriorityInformation").ConfigureAwait(false);
	PrintCsv(line.Name, "PrioritiesReceive", prioritiesReceiveTimestampResult);
	Console.WriteLine();

}

void PrintOperationsIntersectionCsv(string experiment, string name, IList<(string Id, bool isValid)> results)
{
	var fileName = "ValidationResults.csv";
	if (!File.Exists(fileName))
	{
		File.WriteAllLines(fileName, new[] { "Experiment,Name,Id,IsValid" });
	}

	var lines = new List<string>();
	foreach (var result in results)
	{
		var lineBuilder = new StringBuilder();
		lineBuilder.Append(experiment + ",");
		lineBuilder.Append(name + ",");
		lineBuilder.Append(result.Id + ",");
		lineBuilder.Append(result.isValid ? 1 : 0);
		lines.Add(lineBuilder.ToString());
		Console.WriteLine(lines.Last());
	}

	File.AppendAllLines(fileName, lines);
}

void PrintCsv(string experiment, string name, params AnalysisResult[] analysisResults)
{
	var fileName = "AnalysisResults.csv";
	if (!File.Exists(fileName))
	{
		File.WriteAllLines(fileName, new[] { "Experiment,Name,Parameter,Count,Max,Mean,MeanErrorless,StandardDeviation,Variance,Error,Errors" });
	}

	var lines = new List<string>();
	foreach (var analysisResult in analysisResults)
	{
		//.ToString("F50").TrimEnd('0').TrimEnd('.')
		var lineBuilder = new StringBuilder();
		lineBuilder.Append(experiment + ",");
		lineBuilder.Append(name + ",");
		lineBuilder.Append(analysisResult.Parameter + ",");
		lineBuilder.Append(analysisResult.Count + ",");
		lineBuilder.Append(analysisResult.Max + ",");
		lineBuilder.Append(analysisResult.Mean + ",");
		lineBuilder.Append(analysisResult.MeanErrorless + ",");
		lineBuilder.Append(analysisResult.StandardDeviation + ",");
		lineBuilder.Append(analysisResult.Variance + ",");
		lineBuilder.Append(analysisResult.Errors.Count);

		lines.Add(lineBuilder.ToString());
		Console.WriteLine(lines.Last());
	}

	File.AppendAllLines(fileName, lines);
}


void Print(AnalysisResult analysisResult)
{
	Console.WriteLine(@$"Parameter: {analysisResult.Parameter}");
	Console.WriteLine(@$"Count: {TimeSpanHelper.FromNanoseconds(analysisResult.Count)}");
	Console.WriteLine(@$"Max: {TimeSpanHelper.FromNanoseconds(analysisResult.Max)}");
	Console.WriteLine(@$"Mean: {TimeSpanHelper.FromNanoseconds(analysisResult.Mean)}");
	Console.WriteLine(@$"MeanErrorless: {TimeSpanHelper.FromNanoseconds(analysisResult.MeanErrorless)}");
	Console.WriteLine(@$"StandardDeviation: {TimeSpanHelper.FromNanoseconds(analysisResult.StandardDeviation)}");
	Console.WriteLine(@$"Variance: {analysisResult.Variance}");
	Console.WriteLine(@$"Errors: {analysisResult.Errors.Count}: {string.Join(",", analysisResult.Errors.Select(x => $"({x.Value} {x.TraceId})"))}");
	Console.WriteLine();
}
