using Google.Protobuf.WellKnownTypes;
using System.Diagnostics;

namespace RecurrentWorkerService.Distributed.Prioritization.ML.Registration.Models
{
	public struct Influence
	{
		public Influence(byte workload)
		{
			Value = workload;
		}

		public byte Value { get; }

		public static implicit operator byte(Influence d) => d.Value;
		public static implicit operator Influence(byte b) => new(b);


		public static Influence Full => new(byte.MaxValue);

		public static Influence Zero => new(byte.MinValue);

		public static Influence FromPercent(byte percent)
		{
			Debug.Assert(percent <= 100, "Percent should be in range from 0 to 100");

			return new Influence((byte)(byte.MaxValue * percent / 100));
		}

		public float ToPercent()
		{
			return (float)Value / (float)Full;
		}
	}
}
