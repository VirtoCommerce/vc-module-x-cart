using System.Collections.Generic;
using System.Threading;
using MediatR;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Handler and command factories for the CONFIGURATION cluster benchmarks.
///
/// All five handlers target the mutate-existing-cart path: the cart already contains
/// configured line items, so every handler is <see cref="CartShape.Configured"/>-only
/// (<c>[Params(CartShape.Configured)]</c>). A flat cart has no configuration items;
/// running the configuration mutations against it would either short-circuit immediately
/// (not reaching the domain logic) or produce validation errors that abort before
/// the work under measurement — neither produces a meaningful benchmark.
///
/// Design rule (mirrors <see cref="CartBenchmarkFixtures"/>): everything I/O is mocked at
/// the leaf; pure compute (totals calculator, section matching) runs for real.
/// </summary>
internal static class ConfigurationBenchmarkFixtures
{
    // ── AddConfigurationItem ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Real <see cref="AddConfigurationItemCommandHandler"/> over the shared mutation harness.
    /// The harness cart has three Variation <c>ConfigurationItem</c>s per line item
    /// (<c>ci-{i}-0..2</c>, SectionId=<c>null</c>, ProductId=<c>variation-{i}-{v}</c>).
    /// The add command introduces a section with a distinct SectionId so
    /// <c>GetOrCreateConfigurationItem</c> always creates a new item (not an update).
    /// </summary>
    public static AddConfigurationItemCommandHandler CreateAddConfigurationItemHandler(int lineItemCount) =>
        new(CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, CartShape.Configured).Repository);

    /// <summary>
    /// An <c>addConfigurationItem</c> command that adds a new Variation section to <c>li-0</c>.
    ///
    /// Section shape: Type=Variation, SectionId="s-add" (distinct from existing null SectionId
    /// items so <c>GetOrCreateConfigurationItem</c> creates a new item), ProductId=<c>variation-0-0</c>
    /// (the shared <see cref="CartBenchmarkFixtures.CartProductServiceMock"/> resolves any product,
    /// so this succeeds without additional wiring). Passes <c>ValidateConfigurationSections</c>
    /// because <c>Option.ProductId</c> is non-empty.
    /// </summary>
    public static AddConfigurationItemCommand CreateAddConfigurationItemCommand()
    {
        var command = AbstractTypeFactory<AddConfigurationItemCommand>.TryCreateInstance();
        command.LineItemId = "li-0";
        command.ConfigurationSection = CreateVariationSection("s-add", quantity: 1);

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    // ── UpdateConfigurationItem ───────────────────────────────────────────────────────────────────

    /// <summary>Real <see cref="UpdateConfigurationItemCommandHandler"/> over the shared mutation harness.</summary>
    public static UpdateConfigurationItemCommandHandler CreateUpdateConfigurationItemHandler(int lineItemCount) =>
        new(CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, CartShape.Configured).Repository);

    /// <summary>
    /// An <c>updateConfigurationItem</c> command that updates <c>ci-0-0</c> in <c>li-0</c>.
    ///
    /// <c>FindConfigurationItem</c> for Variation type matches by <c>Type + SectionId + ProductId</c>.
    /// The fixture's config items have <c>SectionId=null</c>, so the section uses <c>SectionId=null</c>
    /// and <c>ProductId="variation-0-0"</c> to hit the exact existing item. Passes
    /// <c>ValidateConfigurationSections</c> (ProductId non-empty). The product mock returns a
    /// CartProduct for that ID, so <c>ApplyConfigurationSectionAsync</c> succeeds.
    /// </summary>
    public static UpdateConfigurationItemCommand CreateUpdateConfigurationItemCommand()
    {
        var command = AbstractTypeFactory<UpdateConfigurationItemCommand>.TryCreateInstance();
        command.LineItemId = "li-0";
        command.ConfigurationSection = CreateVariationSection(sectionId: null, quantity: 2);

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    // ── RemoveConfigurationItem ───────────────────────────────────────────────────────────────────

    /// <summary>Real <see cref="RemoveConfigurationItemCommandHandler"/> over the shared mutation harness.</summary>
    public static RemoveConfigurationItemCommandHandler CreateRemoveConfigurationItemHandler(int lineItemCount) =>
        new(CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, CartShape.Configured).Repository);

    /// <summary>
    /// A <c>removeConfigurationItem</c> command that removes <c>ci-0-0</c> from <c>li-0</c>.
    ///
    /// The section matches the existing item by <c>Type + SectionId + ProductId</c> (Variation
    /// rule). The cart is rebuilt fresh per invocation by the never-cache + GetAsync mock, so the
    /// removal is idempotent across benchmark invocations — no [IterationSetup] needed.
    /// </summary>
    public static RemoveConfigurationItemCommand CreateRemoveConfigurationItemCommand()
    {
        var command = AbstractTypeFactory<RemoveConfigurationItemCommand>.TryCreateInstance();
        command.LineItemId = "li-0";
        command.ConfigurationSection = CreateVariationSection(sectionId: null, quantity: 1);

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    // ── ChangeCartConfiguredLineItem ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Real <see cref="ChangeCartConfiguredLineItemCommandHandler"/> over the shared mutation harness,
    /// with an <see cref="IMediator"/> mock that returns a fresh configured line item per send.
    ///
    /// The handler calls <c>mediator.Send(CreateConfiguredLineItemCommand)</c> and uses the returned
    /// <see cref="ExpConfigurationLineItem.Item"/> to replace the line item's configuration. The mock
    /// returns a new item each call (a factory lambda) so the benchmark stays idempotent.
    ///
    /// <b>Local wiring note</b>: this is the only configuration handler that requires an
    /// <see cref="IMediator"/> dependency. The shared <see cref="CartBenchmarkFixtures"/> does not
    /// expose a mediator-aware mutation harness factory (the existing clusters don't need it), so the
    /// harness and handler are assembled here rather than in the shared class. If a future cluster also
    /// needs a mediator-aware harness, this pattern should be moved to
    /// <see cref="CartBenchmarkFixtures"/> as a named overload — FLAG for centralizing.
    /// </summary>
    public static ChangeCartConfiguredLineItemCommandHandler CreateChangeCartConfiguredLineItemHandler(int lineItemCount)
    {
        var harness = CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, CartShape.Configured);

        // The mediator mock returns a fresh configured line item on every send so
        // UpdateConfiguredLineItemAsync doesn't accumulate mutations across invocations.
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(x => x.Send(It.IsAny<CreateConfiguredLineItemCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateConfiguredLineItemCommand command, CancellationToken _) =>
            {
                var item = AbstractTypeFactory<LineItem>.TryCreateInstance();
                item.ProductId = command.ConfigurableProductId;
                item.CatalogId = "catalog";
                item.Sku = $"SKU-{command.ConfigurableProductId}";
                item.Currency = CartBenchmarkFixtures.Currency.Code;
                item.IsConfigured = true;
                item.ConfigurationItems = CartBenchmarkFixtures.CreateConfigurationItems(0);

                return new ExpConfigurationLineItem { Item = item };
            });

        return new ChangeCartConfiguredLineItemCommandHandler(harness.Repository, mediator.Object);
    }

    /// <summary>
    /// A <c>changeCartConfiguredLineItem</c> command targeting the first configured line item
    /// (<c>li-0</c>) with one Variation section. The handler replaces the item's full configuration
    /// via the <c>CreateConfiguredLineItemCommand</c> mediator round-trip. Passing an empty
    /// <c>ConfigurationSections</c> list is also valid (no sections → mediator gets an empty list and
    /// returns a fresh item with the fixture's default config); a non-empty list is used here to keep
    /// the benchmark representative of the typical production code path.
    /// </summary>
    public static ChangeCartConfiguredLineItemCommand CreateChangeCartConfiguredLineItemCommand()
    {
        var command = AbstractTypeFactory<ChangeCartConfiguredLineItemCommand>.TryCreateInstance();
        command.LineItemId = "li-0";
        command.Quantity = 3;
        command.ConfigurationSections = [CreateVariationSection(sectionId: null, quantity: 1)];

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    // ── ChangeCartConfigurationItemSelected ───────────────────────────────────────────────────────

    /// <summary>Real <see cref="ChangeCartConfigurationItemSelectedCommandHandler"/> over the shared mutation harness.</summary>
    public static ChangeCartConfigurationItemSelectedCommandHandler CreateChangeCartConfigurationItemSelectedHandler(int lineItemCount) =>
        new(CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, CartShape.Configured).Repository);

    /// <summary>
    /// A <c>changeCartConfigurationItemSelected</c> command toggling <c>ci-0-0</c> of <c>li-0</c> to
    /// un-selected. The section matches the existing item by <c>Type + SectionId + ProductId</c>
    /// (Variation rule). Fresh cart per call (never-cache) keeps the toggle always from its initial
    /// state (<c>SelectedForCheckout</c> is not set in the fixture → defaults to false already, so
    /// the toggle flips to true then back each reload — no accumulation).
    /// </summary>
    public static ChangeCartConfigurationItemSelectedCommand CreateChangeCartConfigurationItemSelectedCommand()
    {
        var command = AbstractTypeFactory<ChangeCartConfigurationItemSelectedCommand>.TryCreateInstance();
        command.LineItemId = "li-0";
        command.ConfigurationSection = CreateVariationSection(sectionId: null, quantity: 1);
        command.SelectedForCheckout = true;

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    /// <summary>
    /// A Variation configuration section targeting <c>variation-0-0</c>, built via
    /// <see cref="AbstractTypeFactory{T}"/> so a registered <see cref="ProductConfigurationSection"/>
    /// override flows through the same fixtures. <paramref name="sectionId"/> selects the matching
    /// target (<c>null</c> hits an existing config item; a distinct id creates a new one).
    /// </summary>
    private static ProductConfigurationSection CreateVariationSection(string sectionId, int quantity)
    {
        var section = AbstractTypeFactory<ProductConfigurationSection>.TryCreateInstance();
        section.SectionId = sectionId;
        section.Type = "Variation";
        section.Option = new ConfigurableProductOption { ProductId = "variation-0-0", Quantity = quantity };

        return section;
    }
}
