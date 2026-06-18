using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using FluentValidation.Results;
using VirtoCommerce.XCart.Core;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Aggregate-direct microbenchmark of <see cref="CartAggregate.ValidateAsync(string)"/> — the
/// hot path called by <c>CartType.validationErrors</c> resolver on every full-cart GraphQL
/// response. The measured compute = build the <see cref="VirtoCommerce.XCart.Core.Validators.CartValidationContext"/>
/// (via the mocked <see cref="VirtoCommerce.XCart.Core.Validators.ICartValidationContextFactory"/>
/// → includes the AllCartProducts projection), then run the full
/// <see cref="VirtoCommerce.XCart.Core.Validators.CartValidator"/> against the
/// <see cref="ReadLoadBenchmarkFixtures.ItemsRuleSet"/> rule set — exercising per-item
/// <see cref="VirtoCommerce.XCart.Core.Validators.CartLineItemValidator"/> for every selected line
/// item. No I/O: the context factory is a mock that returns a synchronously-built context.
///
/// Idempotent without [IterationSetup]: <see cref="CartAggregate.ValidateAsync(string)"/> caches
/// results per rule-set in <c>ValidationErrorsByRuleSet</c>. To measure the uncached (real) path
/// each invocation, the aggregate is reset by clearing the cache between calls.
///
/// Two axes: <b>shape</b> (Flat vs Configured — configured items carry a ConfigurationItems list
/// that <c>ApplyRuleForOrderCreate</c> → <c>ValidateConfiguredLineItems</c> walks) and cart size.
///
/// Design note: this is the most involved subject. The context factory mock supplies live
/// CartProduct instances (active/buyable/priced) built from the aggregate's own line items so the
/// <c>CartLineItemValidator</c>'s buyability, availability, and quantity rules see real data and
/// produce clean (no-error) results on the success path — measuring the full rule evaluation cost,
/// not a trivially-short-circuiting empty-product path.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Validation)]
public class ValidateCartBenchmarks
{
    private CartAggregate _aggregate = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() =>
        _aggregate = ReadLoadBenchmarkFixtures.BuildValidateCartAggregate(LineItemCount, Shape);

    /// <summary>
    /// Validates the cart with the Items rule set — the per-line-item hot path exercised by
    /// <c>CartType.validationErrors</c> and <c>CartAggregate.GetLineItemValidationErrorsAsync</c>.
    ///
    /// The validation cache is cleared before each call so BDN invokes the real
    /// <see cref="VirtoCommerce.XCart.Core.Validators.CartValidator.ValidateAsync"/> path every time
    /// (not the cached-result short-circuit). The clear is synchronous and cheap (Dictionary.Clear).
    /// </summary>
    [Benchmark]
    public async Task<IList<ValidationFailure>> ValidateCart()
    {
        // Clear the per-ruleSet cache so each BDN invocation runs the real FluentValidation path
        // (rather than returning the Dictionary.TryGetValue cached-result short-circuit).
        _aggregate.ClearValidationCache();

        return await _aggregate.ValidateAsync(ReadLoadBenchmarkFixtures.ItemsRuleSet);
    }
}
