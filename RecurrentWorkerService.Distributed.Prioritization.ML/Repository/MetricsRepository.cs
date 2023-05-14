using System.Text.Json;
using RecurrentWorkerService.Distributed.Prioritization.ML.Repository.Models;

namespace RecurrentWorkerService.Distributed.Prioritization.ML.Repository
{
	internal class MetricsRepository : IMetricsRepository
	{
		public MetricsRepository(string filepath, int capacity)
		{
			_filepath = filepath;
			_capacity = capacity;

			if (File.Exists(_filepath))
			{
				_data = JsonSerializer.Deserialize<List<Metrics>>(File.ReadAllBytes(_filepath)) ?? new List<Metrics>(_capacity);
			}
			else
			{
				_data = new List<Metrics>(_capacity);
			}
		}

		public void Add(Metrics metric)
		{
			lock (_data)
			{
				_data.Add(metric);
				_data.RemoveRange(0, Math.Max(_data.Count - _capacity, 0));
				File.WriteAllBytes(_filepath, JsonSerializer.SerializeToUtf8Bytes(_data));
			}
		}

		public Metrics[] GetLast(int count)
		{
			lock (_data)
			{
				return _data.TakeLast(count).ToArray();
			}
		}

		public Metrics[] GetAll()
		{
			lock (_data)
			{
				return _data.ToArray();
			}
		}

		private readonly List<Metrics> _data;

		private readonly string _filepath;
		private readonly int _capacity;
	}
}
