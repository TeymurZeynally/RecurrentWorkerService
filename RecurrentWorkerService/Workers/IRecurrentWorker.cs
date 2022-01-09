namespace RecurrentWorkerService.Workers;

public interface IRecurrentWorker
{
	Task ExecuteAsync(CancellationToken cancellationToken);
}