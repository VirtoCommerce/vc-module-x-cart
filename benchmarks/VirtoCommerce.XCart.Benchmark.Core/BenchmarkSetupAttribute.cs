using System;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Declares which <see cref="ICartModuleBenchmarkSetup"/> a benchmark runner exe bakes into the concrete
/// subclasses the source generator emits — one per Core <c>*BenchmarksBase</c>. Apply once at assembly
/// scope in the runner:
/// <code>[assembly: BenchmarkSetup(typeof(UpstreamCartBenchmarkSetup))]</code>
/// The generator reads this attribute, walks the Core assembly for every abstract <c>*BenchmarksBase</c>,
/// and emits a sealed concrete subclass per base (name with the "Base" suffix dropped) whose
/// <see cref="CartBenchmarkBase.CreateSetup"/> returns <c>new TSetup()</c>. BenchmarkDotNet then
/// discovers those subclasses in the runner assembly.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class BenchmarkSetupAttribute : Attribute
{
    public BenchmarkSetupAttribute(Type setupType)
    {
        SetupType = setupType;
    }

    /// <summary>The <see cref="ICartModuleBenchmarkSetup"/> implementation to bake into every subclass.</summary>
    public Type SetupType { get; }
}
