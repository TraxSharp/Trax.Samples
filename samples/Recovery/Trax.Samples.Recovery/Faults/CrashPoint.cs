namespace Trax.Samples.Recovery.Faults;

/// <summary>Where an armed fault fires.</summary>
public enum CrashPoint
{
    /// <summary>No crash: the run completes on its first attempt.</summary>
    None,

    /// <summary>The research agent's second tool call, after both model calls.</summary>
    ToolCall,

    /// <summary>The refund's payment step, after the approval decision.</summary>
    Payment,
}
