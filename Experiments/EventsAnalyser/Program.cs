using System.Runtime.InteropServices;
using System.Text;
using EventsAnalyser.Analysers;
using EventsAnalyser.Calculators.Models;
using EventsAnalyser.Helpers;
using EventsAnalyser.Queries.Models;
using FluentAssertions;
using InfluxDB.Client;

using Interval = EventsAnalyser.Queries.Models.Interval;

var planExecutionsFile = @"/home/teymur/Desktop/Dashboard/ExperimentRuns.csv";

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
};




foreach (var line in lines)
{
	Console.WriteLine();
	Console.WriteLine(line.Name);
	Console.WriteLine();	
	
	var periodicOperationsReults = new List<AnalysisResult>();
	var libAndCodeOperationsReults = new List<AnalysisResult>();
	var periodicOperationsIntersectionResults = new List<(string Name, bool IsValid)>();

	foreach (var config in configurationOfWorkers)
	{
		var periodicOperationsIntersectionResult = await new PeriodicOperationsIntersectionAnalyser(queryApi).Analyse(line.Interval, config.Period, config.Payload, config.Id);
		var periodicOperationsResult = await new PeriodicOperationsAnalyser(queryApi).Analyse(line.Interval, config.Period, config.Payload, config.Id);
		var libAndCodeOperationsAnalyserResult = await new LibAndCodeOperationsAnalyser(queryApi).Analyse(line.Interval, "DistributedRecurrentWorkerService",  config.Payload, config.Id);

		periodicOperationsIntersectionResult.Should().BeTrue();
		periodicOperationsIntersectionResults.Add((config.Id, periodicOperationsIntersectionResult));
		periodicOperationsReults.Add(periodicOperationsResult);
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
	var fileName = "periodicOperationsValidationResults.csv";
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
		lineBuilder.Append(result.isValid);
		lines.Add(lineBuilder.ToString());
		Console.WriteLine(lines.Last());
	}

	File.AppendAllLines(fileName, lines);
}

void PrintCsv(string experiment, string name, params AnalysisResult[] analysisResults)
{
	var fileName = "output.csv";
	if (!File.Exists(fileName))
	{
		File.WriteAllLines(fileName, new[] { "Experiment,Name,Parameter,Count,Max,Mean,MeanErrorless,StandardDeviation,Variance,Error,Errors" });
	}

	var lines = new List<string>();
	foreach (var analysisResult in analysisResults)
	{
		var lineBuilder = new StringBuilder();
		lineBuilder.Append(experiment + ",");
		lineBuilder.Append(name + ",");
		lineBuilder.Append(analysisResult.Parameter + ",");
		lineBuilder.Append(analysisResult.Count + ",");
		lineBuilder.Append(analysisResult.Max.ToString("F50").TrimEnd('0').TrimEnd('.')+ ",");
		lineBuilder.Append(analysisResult.Mean.ToString("F50").TrimEnd('0').TrimEnd('.')+ ",");
		lineBuilder.Append(analysisResult.MeanErrorless.ToString("F50").TrimEnd('0').TrimEnd('.')+ ",");
		lineBuilder.Append(analysisResult.StandardDeviation.ToString("F50").TrimEnd('0').TrimEnd('.')+ ",");
		lineBuilder.Append(analysisResult.Variance.ToString("F50").TrimEnd('0').TrimEnd('.')+ ",");
		lineBuilder.Append(analysisResult.Error.ToString("F50").TrimEnd('0').TrimEnd('.')+ ",");
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
	Console.WriteLine(@$"Error: {TimeSpanHelper.FromNanoseconds(analysisResult.Error)}");
	Console.WriteLine(@$"Errors: {analysisResult.Errors.Count}: {string.Join(",", analysisResult.Errors.Select(x => $"({x.Value} {x.TraceId})"))}");
	Console.WriteLine();
}
