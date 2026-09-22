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
/// aggregate (base or a consumer's subclass) from <c>Func&lt;CartAggregate&gt;</c>; the host's default
/// <c>ICartValidationContextFactory</c> supplies real CartProduct data, so this measures the full rule
/// evaluation rather than a short-circuiting empty-product path.
///
/// Idempotency needs more than the shared fresh-cart mock here: <see cref="CartAggregate.ValidateAsync(string)"/>
/// caches per rule-set, so the benchmark clears that cache each invocation to measure the uncached path.
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
        var provider = BuildProvider(LineItemCount, Shape);

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
