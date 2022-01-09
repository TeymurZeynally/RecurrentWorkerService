namespace RecurrentWorkerService.Workers;

public interface ICronWorker
{
	Task ExecuteAsync(CancellationToken cancellationToken);
}