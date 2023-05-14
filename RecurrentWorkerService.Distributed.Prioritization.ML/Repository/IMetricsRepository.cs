using RecurrentWorkerService.Distributed.Prioritization.ML.Repository.Models;

namespace RecurrentWorkerService.Distributed.Prioritization.ML.Repository;

internal interface IMetricsRepository
{
	void Add(Metrics metric);

	Metrics[] GetLast(int count);

	Metrics[] GetAll();
}