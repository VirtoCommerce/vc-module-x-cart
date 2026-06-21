using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Standalone microbenchmark of <see cref="CartAggregate.RecalculateAsync"/> — the cart hot path
/// (fires on every read via the aggregate cache and inside every mutation's save) against the real
/// <c>DefaultShoppingCartTotalsCalculator</c>. Aggregate-direct: resolves the concrete aggregate
/// (base or a consumer's subclass) from <c>Func&lt;CartAggregate&gt;</c> so a module override is
/// measured. Read the <c>Allocated</c> column across the count/shape rows; count 100 surfaces O(n²).
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Recalculate)]
public abstract class RecalculateAsyncBenchmarksBase : CartBenchmarkBase
{
    private CartAggregate _aggregate = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _aggregate = BuildProvider(LineItemCount, Shape).GetRequiredService<Func<CartAggregate>>()();

        var cart = CartBenchmarkFixtures.CreateCart(LineItemCount, Shape);
        _aggregate.GrabCart(cart, CartBenchmarkFixtures.CreateStore(), member: null, CartBenchmarkFixtures.Currency);
    }

    [Benchmark]
    public Task<CartAggregate> RecalculateAsync() => _aggregate.RecalculateAsync();
}
