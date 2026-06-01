namespace Trax.Samples.Shared.Data.Tests.Fakes;

/// <summary>A minimal owned entity used to exercise the shared base context.</summary>
public class Widget
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
