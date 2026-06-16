using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Validators;
using VirtoCommerce.XCart.Tests.Helpers;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Performance
{
    /// <summary>
    /// Before/after demo for the GetFullCart validation stampede. Drives the REAL CartAggregate the
    /// way GraphQL does — every line item's isValid/validationErrors resolver concurrently calls
    /// GetLineItemValidationErrorsAsync -> ValidateAsync("Items") — while modeling the catalog index
    /// as a bounded, latency-bearing dependency. Prints product-search count and wall-clock so you
    /// can capture numbers on `dev` (before) and the fix branch (after).
    ///
    /// Run after  : dotnet test --filter FullyQualifiedName~CartValidationStampedeDemo
    /// Run before : git checkout dev -- src/VirtoCommerce.XCart.Core/CartAggregate.cs  (then run, then restore)
    /// </summary>
    public class CartValidationStampedeDemo : XCartMoqHelper
    {
        private const int LineItemCount = 40;
        private const int CatalogIndexMaxConcurrency = 4;   // models a bounded ES connection pool / cluster capacity
        private const int ProductSearchLatencyMs = 120;     // observed per-call latency under load (prod: ~123 ms)

        private readonly ITestOutputHelper _output;

        public CartValidationStampedeDemo(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task GetFullCart_PerLineValidation_ProductSearchesDoNotScaleWithCartSize()
        {
            // Arrange - a cart with many line items and the catalog index modeled as a slow, bounded dependency.
            var index = new BoundedCatalogIndexFake(CatalogIndexMaxConcurrency, ProductSearchLatencyMs);
            var aggregate = BuildAggregate(index, LineItemCount);

            // Act - reproduce GraphQL's concurrent per-line resolution of isValid + validationErrors.
            var sw = Stopwatch.StartNew();
            await Task.WhenAll(aggregate.Cart.Items.SelectMany(li => new[]
            {
                aggregate.GetLineItemValidationErrorsAsync(li),   // isValid resolver
                aggregate.GetLineItemValidationErrorsAsync(li),   // validationErrors resolver
            }));
            sw.Stop();

            // Report
            _output.WriteLine($"line items            : {LineItemCount}");
            _output.WriteLine($"product index searches: {index.SearchCount}");
            _output.WriteLine($"GetFullCart wall-clock: {sw.ElapsedMilliseconds} ms");

            // Guard - one validation pass per cart, regardless of size (fails on pre-fix code: ~{LineItemCount}).
            Assert.True(index.SearchCount <= CatalogIndexMaxConcurrency,
                $"Expected the catalog index to be hit a small constant number of times, but it was hit " +
                $"{index.SearchCount} times for {LineItemCount} line items - the per-line validation stampede is back.");
        }

        private CartAggregate BuildAggregate(ICartValidationContextFactory validationContextFactory, int lineItemCount)
        {
            var aggregate = new CartAggregate(
                _marketingPromoEvaluatorMock.Object,
                _shoppingCartTotalsCalculatorMock.Object,
                _taxProviderSearchServiceMock.Object,
                _cartProductServiceMock.Object,
                _dynamicPropertyUpdaterService.Object,
                _mapperMock.Object,
                _memberService.Object,
                _genericPipelineLauncherMock.Object,
                _configurationItemValidatorMock.Object,
                _fileUploadService.Object,
                _cartSharingService.Object,
                validationContextFactory);

            var currency = GetCurrency();
            var cart = GetCart();
            cart.Items = Enumerable.Range(0, lineItemCount)
                .Select(i => new LineItem
                {
                    Id = $"li-{i}",
                    ProductId = $"prod-{i}",
                    Currency = currency.Code,
                    Quantity = 1,
                    SelectedForCheckout = true,
                })
                .ToList<LineItem>();

            aggregate.GrabCart(cart, GetStore(), GetMember(), currency);
            return aggregate;
        }

        /// <summary>
        /// Models the catalog search index: every CreateValidationContextAsync call represents one
        /// product search, throttled to <paramref name="maxConcurrency"/> in-flight calls, each taking
        /// <paramref name="latencyMs"/>. This reproduces the real saturation behaviour (bounded pool +
        /// per-call latency) without a live Elasticsearch.
        /// </summary>
        private sealed class BoundedCatalogIndexFake : ICartValidationContextFactory
        {
            private readonly SemaphoreSlim _capacity;
            private readonly int _latencyMs;
            private int _searchCount;

            public BoundedCatalogIndexFake(int maxConcurrency, int latencyMs)
            {
                _capacity = new SemaphoreSlim(maxConcurrency, maxConcurrency);
                _latencyMs = latencyMs;
            }

            public int SearchCount => _searchCount;

            public async Task<CartValidationContext> CreateValidationContextAsync(CartAggregate cartAggregate)
            {
                await _capacity.WaitAsync();
                try
                {
                    Interlocked.Increment(ref _searchCount);
                    await Task.Delay(_latencyMs);
                    return new CartValidationContext();
                }
                finally
                {
                    _capacity.Release();
                }
            }

            public Task<CartValidationContext> CreateValidationContextAsync(CartAggregate cartAggregate, IList<CartProduct> products)
                => CreateValidationContextAsync(cartAggregate);
        }
    }
}
