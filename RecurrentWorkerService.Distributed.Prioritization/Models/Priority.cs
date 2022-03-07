namespace RecurrentWorkerService.Distributed.Prioritization.Models;

internal struct Priority
{
	public Priority(byte priority)
	{
		Value = priority;
	}

	public byte Value { get; }

	public static implicit operator byte(Priority d) => d.Value;
	public static implicit operator Priority(byte b) => new(b);

	public static Priority Low => new(byte.MaxValue);

	public static Priority High => new(byte.MinValue);

	public override bool Equals(object? obj)
	{
		if (obj != null && obj is Priority p)
		{
			return Value.Equals(p.Value);
		}

		return base.Equals(obj);
	}

	public override int GetHashCode()
	{
		return Value.GetHashCode();
	}

	public override string ToString()
	{
		return Value.ToString();
	}
}