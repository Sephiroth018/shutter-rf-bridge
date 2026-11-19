namespace ShutterRfBridge.Tests.TestSupport;

/// <summary>A made-up remote for tests; the real remote's ID never goes into the repository.</summary>
internal static class MadeUpRemote
{
    // High bits set and no symmetry, so a bit-order mistake changes the result.
    public const uint Id = 0xA5C3E71;
}
