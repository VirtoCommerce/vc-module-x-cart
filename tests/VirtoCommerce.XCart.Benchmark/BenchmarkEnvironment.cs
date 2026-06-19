namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Ambient selector for the active <see cref="ICartModuleBenchmarkSetup"/>. Fixtures read
/// <see cref="Current"/> to build the module-correct cart aggregate.
///
/// <para>It defaults to the upstream (un-extended XCart) setup defined in this assembly. This default
/// is what makes the BenchmarkDotNet out-of-process toolchain work: the generated child process
/// references only the benchmark assembly (this one), never a consuming module's runner, so it can only
/// resolve a setup that lives here. The default is materialized lazily on first access, which happens
/// inside the benchmark's <c>[GlobalSetup]</c> in the child process.</para>
///
/// <para>A consuming module's runner that runs benchmarks <b>in-process</b> (same process as its
/// <c>Main</c>) overrides <see cref="Current"/> before <c>BenchmarkSwitcher.Run</c>; the assignment is
/// then visible to the fixtures because there is no separate child process.</para>
/// </summary>
public static class BenchmarkEnvironment
{
    private static ICartModuleBenchmarkSetup _current;

    public static ICartModuleBenchmarkSetup Current
    {
        get => _current ??= new UpstreamCartBenchmarkSetup();
        set => _current = value;
    }
}
