namespace ExperimentRunner.Models;

public class NetworkParameters
{
    public string? Bandwidth { get; init; }
    
    public int? DelayMs { get; init; }
    
    public byte? LossPercent { get; init; }
    
    public byte? DuplicatePercent { get; init; }

    public byte? CorruptPercent { get; init; }
}
