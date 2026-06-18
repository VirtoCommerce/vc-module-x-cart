namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Category tags for tiered gating. Per-PR CI runs <c>--anyCategories Tier1</c> (the hottest
/// paths only); the nightly job runs the full matrix. Keeps per-commit cost bounded while still
/// catching regressions on the operations that run most often.
/// </summary>
public static class BenchmarkCategories
{
    public const string Tier1 = nameof(Tier1);
    public const string Tier2 = nameof(Tier2);
}
