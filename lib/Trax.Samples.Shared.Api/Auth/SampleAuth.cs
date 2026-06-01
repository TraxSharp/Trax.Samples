namespace Trax.Samples.Shared.Api.Auth;

/// <summary>
/// Conventions shared by the samples' demonstration auth setups.
/// </summary>
public static class SampleAuth
{
    /// <summary>
    /// Suffix every sample demo API key carries so it can never be mistaken for a real secret and
    /// so a meta-test can prove no real-looking credential was checked in. Example:
    /// <c>"admin" + DemoKeySuffix</c>.
    /// </summary>
    public const string DemoKeySuffix = "-key-do-not-use-in-production";
}
