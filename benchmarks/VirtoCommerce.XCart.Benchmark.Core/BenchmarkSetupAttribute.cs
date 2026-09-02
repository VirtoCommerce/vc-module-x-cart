using System;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Declares which <see cref="ICartBenchmarkSetup"/> a benchmark runner exe bakes into the concrete
/// subclasses the source generator emits. Apply once at assembly scope in the runner:
/// <code>[assembly: BenchmarkSetup(typeof(XCartBenchmarkSetup))]</code>
/// A runner that omits it gets an empty suite — the generator emits nothing, and BenchmarkDotNet
/// reports "Found 0 benchmarks" rather than failing the build.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class BenchmarkSetupAttribute : Attribute
{
    public BenchmarkSetupAttribute(Type setupType)
    {
        SetupType = setupType;
    }

    /// <summary>The <see cref="ICartBenchmarkSetup"/> implementation to bake into every subclass.</summary>
    public Type SetupType { get; }
}
