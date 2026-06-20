namespace VirtoCommerce.XCart.Benchmark;

// TEMPORARY hand-written concrete subclasses to validate the DI-resolved inheritance model
// out-of-process before the source generator (2d) replaces them. Each bakes the upstream module
// setup; BDN discovers them in this exe and rebuilds this exe's .csproj for the child process.
public class ChangeCartItemQuantityBenchmarks : ChangeCartItemQuantityBenchmarksBase
{
    protected override ICartModuleBenchmarkSetup CreateSetup() => new UpstreamCartBenchmarkSetup();
}

public class RecalculateAsyncBenchmarks : RecalculateAsyncBenchmarksBase
{
    protected override ICartModuleBenchmarkSetup CreateSetup() => new UpstreamCartBenchmarkSetup();
}
