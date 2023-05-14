using RecurrentWorkerService.Distributed.Prioritization.Indicators;

namespace Application
{
	internal class PriorityIndicator : IPriorityIndicator
	{
		private readonly Random _random;

		public PriorityIndicator()
		{
			_random = new Random();
		}

		public byte GetMeasurement()
		{
			return (byte)_random.Next(byte.MinValue, byte.MaxValue);
		}
	}
}
