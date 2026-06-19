using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Data.Queries;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Query-level microbenchmark of the <c>getPricesSum</c> GraphQL query
/// (<see cref="GetPricesSumQueryHandler.Handle"/>): a two-aggregate path. The measured compute:
/// (1) load the source cart by CartId (real load + recalc); (2) build a fresh temp aggregate
/// via <c>GetCartForShoppingCartAsync</c> (real build + recalc); (3) copy line items from source
/// to temp via <c>AddItemsAsync</c>; (4) <c>RecalculateAsync</c> on the temp aggregate; (5) read
/// the totals. Only I/O leaves are mocked (DB read, never-cache). Both aggregate builds run the
/// real <c>DefaultShoppingCartTotalsCalculator</c> — so this benchmark measures 2× the recalc
/// cost of <see cref="GetCartBenchmarks"/> plus the copy overhead.
///
/// Two axes: <b>shape</b> (Flat vs Configured) and cart size. A Configured cart additionally
/// resolves variation products on load, making the two-aggregate overhead visible on that shape.
///
/// Note: the <see cref="GetPricesSumQuery.LineItemIds"/> covers all items in the cart so that the
/// copy path fully exercises the item-level compute surface (not a trivially-empty subset).
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Queries)]
public class GetPricesSumBenchmarks
{
    private GetPricesSumQueryHandler _handler = null!;
    private GetPricesSumQuery _query = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _handler = ReadLoadBenchmarkFixtures.CreateGetPricesSumHandler(LineItemCount, Shape);
        _query = ReadLoadBenchmarkFixtures.CreateGetPricesSumQuery(LineItemCount);
    }

    [Benchmark]
    public Task<ExpPricesSum> GetPricesSum() => _handler.Handle(_query, CancellationToken.None);
}
