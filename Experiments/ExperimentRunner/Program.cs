using System.Diagnostics;
using System.Text;
using ExperimentRunner.Models;
using Microsoft.Extensions.Configuration;


var configuration = new ConfigurationBuilder().AddJsonFile($"appsettings.json").Build().Get<Configuration>();

await RunFastCommandAndWait($"docker container rm -f etcd_events_collector").ConfigureAwait(false);
var kvEtcdHosts = string.Join(";", configuration!.Plan.SelectMany(x => x.Storages.Select(k => k.Name)).Distinct().Select(x => $"{x}:2379").ToArray());
await RunFastCommandAndWait($"docker run -d --name etcd_events_collector --network experiment -v /home/teymur/Desktop/Dashboard/DockerInfluxLineFilesVolume:/influx -e EXPERIMENT_ETCD_HOSTS={kvEtcdHosts} etcd_events_collector").ConfigureAwait(false);

foreach (var planItem in configuration.Plan)
{
    var applicationProcesses = GetApplicationsProcesses(planItem.Applications, planItem.Storages.Select(x=> x.Name).ToArray());
    var storageProcesses = GetStorageProcesses(planItem.Storages);

    var startTimestamp = DateTimeOffset.UtcNow;
    var cancellationTokenSource = new CancellationTokenSource(planItem.TestDuration);

    var tasks = storageProcesses
        .Union(applicationProcesses)
        .Select(x => Task.Run(async () =>
        {
            Console.WriteLine($"{x.StartInfo.FileName} {x.StartInfo.Arguments}");
            var start = x.Start();
            if (!start) throw new InvalidOperationException($"Process '{x.StartInfo.FileName} {x.StartInfo.Arguments}' can not be started");
            await x.WaitForExitAsync().ConfigureAwait(false);
            if (x.ExitCode != 0) throw new InvalidOperationException($"Process '{x.StartInfo.FileName} {x.StartInfo.Arguments}' exited with code {x.ExitCode}");
            var containerId = (await x.StandardOutput.ReadToEndAsync().ConfigureAwait(false)).Trim();
        
            if (!cancellationTokenSource.Token.WaitHandle.WaitOne())
                throw new InvalidOperationException($"CT problem in '{x.StartInfo.FileName} {x.StartInfo.Arguments}': WaitHandle is false");

            await RunFastCommandAndWait($"docker container rm -f {containerId}").ConfigureAwait(false);
        }));
    
    Task.WaitAll(tasks.ToArray());
    var endTimestamp = DateTimeOffset.UtcNow;

    if (!File.Exists(configuration.PlanExecutionsFile)) File.Create(configuration.PlanExecutionsFile);
    await File.AppendAllLinesAsync(configuration.PlanExecutionsFile, new[] { $"{startTimestamp:O},{endTimestamp:O},{planItem.Name}" }).ConfigureAwait(false);

    await Task.Delay(configuration.DelayBetweenTests).ConfigureAwait(false);
}

async Task RunFastCommandAndWait(string command)
{
    var fileName = command.Split(" ").First();
    var arguments = string.Join(" ", command.Split(" ").Skip(1).ToArray());

    var process = new Process { StartInfo = { FileName = fileName, Arguments = arguments } };
    var start = process.Start();
    if (!start) throw new InvalidOperationException($"Process '{process.StartInfo.FileName} {process.StartInfo.Arguments}' can not be started");
    await process.WaitForExitAsync().ConfigureAwait(false);
    if (process.ExitCode != 0) throw new InvalidOperationException($"Process '{process.StartInfo.FileName} {process.StartInfo.Arguments}' exited with code {process.ExitCode}");
}

string GetNetDockerTcLabels(NetworkParameters? parameters)
{
    if(parameters == null) return string.Empty;
    
    var sb = new StringBuilder();
    sb.Append("--label \"com.docker-tc.enabled=1\" ");
	if(parameters.Bandwidth != null) sb.Append($"--label \"com.docker-tc.limit={parameters.Bandwidth}\" ");
	if(parameters.DelayMs != null) sb.Append($"--label \"com.docker-tc.delay={parameters.DelayMs}ms\" ");
	if(parameters.LossPercent != null) sb.Append($"--label \"com.docker-tc.loss={parameters.LossPercent}%\" ");
	if(parameters.DuplicatePercent != null) sb.Append($"--label \"com.docker-tc.duplicate={parameters.DuplicatePercent}%\" ");
	if(parameters.CorruptPercent != null) sb.Append($"--label \"com.docker-tc.corrupt={parameters.CorruptPercent}%\" ");
    return sb.ToString();
}

Process[] GetStorageProcesses(DockerApplication[] applications)
{
    var initialCluster = string.Join(",", applications.Select(x => $"{x.Name}=http://{x.Name}:2380"));
    var parameters = applications.Select(x =>
        $"run -d --network experiment {GetNetDockerTcLabels(x.NetworkSettings)} --name={x.Name} quay.io/coreos/etcd /usr/local/bin/etcd -name {x.Name} -advertise-client-urls http://{x.Name}:2379 -listen-client-urls http://0.0.0.0:2379 -initial-advertise-peer-urls http://{x.Name}:2380 -listen-peer-urls http://0.0.0.0:2380 -initial-cluster-token etcd-cluster-1 -initial-cluster {initialCluster} -initial-cluster-state new");
    return parameters.Select(x => new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = x,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        })
        .ToArray();
}

Process[] GetApplicationsProcesses(DockerApplication[] applications, string[] kvHosts)
{
    var kvEtcdHosts = string.Join(";", kvHosts.Select(x => $"{x}:2379"));
    return applications.Select(x => new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"run -d --name={x.Name} --network experiment {GetNetDockerTcLabels(x.NetworkSettings)} -v /home/teymur/Desktop/Dashboard/DockerInfluxLineFilesVolume:/influx -e EXPERIMENT_ENV=docker -e EXPERIMENT_ETCD_HOSTS={kvEtcdHosts} experimental_application",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        })
        .ToArray();
}