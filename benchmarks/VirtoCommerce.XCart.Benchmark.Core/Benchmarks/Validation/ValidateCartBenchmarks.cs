using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Aggregate-direct microbenchmark of <see cref="CartAggregate.ValidateAsync(string)"/> — the hot path
/// called by <c>CartType.validationErrors</c> on every full-cart GraphQL response. Resolves the concrete
/// aggregate (base or a consumer's subclass) from <c>Func&lt;CartAggregate&gt;</c>; the validation
/// benchmark overrides the host's loose <c>ICartValidationContextFactory</c> with a working one (via the
/// <c>customizeServices</c> hook) so <c>CartValidator</c>'s per-item rules run on real CartProduct data,
/// measuring the full rule evaluation rather than a short-circuiting empty-product path.
///
/// Idempotent: <see cref="CartAggregate.ValidateAsync(string)"/> caches per rule-set, so the benchmark
/// clears the cache each invocation to measure the real (uncached) path. Two axes: shape (Flat vs
/// Configured) and cart size.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Validation)]
public abstract class ValidateCartBenchmarksBase : CartBenchmarkBase
{
    private CartAggregate _aggregate = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var provider = BuildProvider(
            LineItemCount,
            Shape,
            customizeServices: services => services.AddSingleton(ReadLoadBenchmarkFixtures.CreateValidationContextFactory()));

        _aggregate = provider.GetRequiredService<Func<CartAggregate>>()();

        var cart = CartBenchmarkFixtures.CreateCart(LineItemCount, Shape);
        _aggregate.GrabCart(cart, CartBenchmarkFixtures.CreateStore(), member: null, CartBenchmarkFixtures.Currency);

        // Settle totals synchronously — GlobalSetup cannot await.
        _aggregate.RecalculateAsync().GetAwaiter().GetResult();
    }

    [Benchmark]
    public async Task<IList<ValidationFailure>> ValidateCart()
    {
        // Clear the per-ruleSet cache so each invocation runs the real FluentValidation path.
        _aggregate.ClearValidationCache();

        return await _aggregate.ValidateAsync(ReadLoadBenchmarkFixtures.ItemsRuleSet);
    }
}
