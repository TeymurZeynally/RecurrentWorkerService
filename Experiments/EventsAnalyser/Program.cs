using System.Runtime.InteropServices;
using System.Text;
using EventsAnalyser.Analysers;
using EventsAnalyser.Calculators.Models;
using EventsAnalyser.Helpers;
using EventsAnalyser.Queries.Models;
using FluentAssertions;
using InfluxDB.Client;

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


var configurationOfWorkers = new[]
{
	(Id: "Cron-Immediate", Payload: PayloadType.Immediate, Period: TimeSpan.FromMinutes(1)),
	(Id: "Cron-Fast", Payload: PayloadType.Fast, Period: TimeSpan.FromMinutes(1)),
	(Id: "Cron-Slow", Payload: PayloadType.Slow, Period: TimeSpan.FromMinutes(1)),
	(Id: "Recurrent-Immediate", Payload: PayloadType.Immediate, Period: TimeSpan.FromTicks(1)),
	(Id: "Recurrent-Fast", Payload: PayloadType.Fast, Period: TimeSpan.FromSeconds(1)),
	(Id: "Recurrent-Slow", Payload: PayloadType.Slow, Period: TimeSpan.FromSeconds(1)),
	(Id: "Workload-Immediate", Payload: PayloadType.Immediate, Period: default(TimeSpan?)),
	(Id: "Workload-Fast", Payload: PayloadType.Fast, Period: default(TimeSpan?)),
	(Id: "Workload-Slow", Payload: PayloadType.Slow, Period: default(TimeSpan?)),
};

foreach (var line in lines)
{
	Console.WriteLine();
	Console.WriteLine(line.Name);
	Console.WriteLine();

	var excludeList = new string[] {
//		"Test 5.3: Storage delay 500ms",
//		"Test 5.4: Storage delay 700ms",
//		"Test 5.5: Storage delay 900ms",
//		"Test 5.6: Storage delay 1100ms",
//		"Test 8.3: Storage loss percent 50%",
//		"Test 14.3: Storage corrupt percent 50%"
	};

	var excludeAssertOperationsIntersection = new string[] {
//		"Test 7.2: App loss percent 30%",
//		"Test 7.3: App loss percent 50%",
//		"Test 9.2: App loss percent 50% on two nodes",
//		"Test 13.2: App corrupt percent 30%",
//		"Test 13.3: App corrupt percent 50%",
//		"Test 15.1: App corrupt percent 50% on one node",
//		"Test 15.2: App corrupt percent 50% on twp nodes"
	};

	if (excludeList.Contains(line.Name)) continue;
	
	var periodicOperationsReults = new List<AnalysisResult>();
	var libAndCodeOperationsReults = new List<AnalysisResult>();
	var periodicOperationsIntersectionResults = new List<(string Name, bool IsValid)>();

	foreach (var config in configurationOfWorkers)
	{
		var periodicOperationsIntersectionResult = await new PeriodicOperationsIntersectionAnalyser(queryApi).Analyse(line.Interval, config.Payload, config.Id);
		var libAndCodeOperationsAnalyserResult = await new LibAndCodeOperationsAnalyser(queryApi).Analyse(line.Interval, "DistributedRecurrentWorkerService", config.Payload, config.Id);

		if(config.Period != null)
		{
			var periodicOperationsResult = await new PeriodicOperationsAnalyser(queryApi).Analyse(line.Interval, config.Period.Value, config.Payload, config.Id);
			periodicOperationsReults.Add(periodicOperationsResult);
		}

		if (!excludeAssertOperationsIntersection.Contains(line.Name)) periodicOperationsIntersectionResult.Should().BeTrue();
		periodicOperationsIntersectionResults.Add((config.Id, periodicOperationsIntersectionResult));
		libAndCodeOperationsReults.Add(libAndCodeOperationsAnalyserResult);
	}


	PrintOperationsIntersectionCsv(line.Name, "PeriodicOperationsIntersection", periodicOperationsIntersectionResults);

	PrintCsv(line.Name, "PeriodicOperations", periodicOperationsReults.ToArray());
	Console.WriteLine();
	PrintCsv(line.Name, "LibAndCodeOperations", libAndCodeOperationsReults.ToArray());
	Console.WriteLine();

	var persistenceOperationsDurationResults = await new PersistenceOperationsDurationAnalyser(queryApi).Analyse(line.Interval);
	PrintCsv(line.Name, "PersistenceOperationsDuration", persistenceOperationsDurationResults);
	Console.WriteLine();

	var prioritiesReceiveTimestampResult = await new PrioritiesReceiveTimestampAnalyser(queryApi).Analyse(line.Interval, "UpdatePriorityInformation");
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
