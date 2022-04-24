using System.Diagnostics;

namespace RecurrentWorkerService.Workers.Models;

public struct Workload
{
	public Workload(byte workload)
	{
		Value = workload;
	}

	public byte Value { get; }

	public static implicit operator byte(Workload d) => d.Value;
	public static implicit operator Workload(byte b) => new(b);

	public static Workload Full => new(byte.MaxValue);

	public static Workload Zero => new(byte.MinValue);

	public static Workload FromPercent(byte percent)
	{
		Debug.Assert(percent <= 100, "Percent should be in range from 0 to 100");

		return new Workload((byte)(byte.MaxValue * percent / 100));
	}

	public static Workload FromDoneItems(int doneItems, int maxItems)
	{
		Debug.Assert(doneItems >= 0, "DoneItems should be greater or equal to 0");
		Debug.Assert(maxItems >= 0, "DoneItems should be greater or equal to 0");

		return maxItems == 0 ? Zero : new((byte)(byte.MaxValue * doneItems / maxItems));
	}
}