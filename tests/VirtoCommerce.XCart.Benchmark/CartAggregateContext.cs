using AutoMapper;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.MarketingModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.TaxModule.Core.Services;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Core.Validators;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// The cart-aggregate dependencies that are identical across modules — the real totals calculator and
/// the mocked I/O leaves the fixtures wire per scenario. An <see cref="ICartModuleBenchmarkSetup"/>
/// consumes these and supplies its own module-specific dependencies (the recalculate pipeline launcher,
/// and any subclass-only services) when constructing the concrete aggregate. The pipeline launcher is
/// deliberately NOT here: upstream mocks it, a consuming module provides a real one.
/// </summary>
public sealed record CartAggregateContext(
    IMarketingPromoEvaluator MarketingEvaluator,
    IShoppingCartTotalsCalculator TotalsCalculator,
    IOptionalDependency<ITaxProviderSearchService> TaxProviderSearchService,
    ICartProductService CartProductService,
    IDynamicPropertyUpdaterService DynamicPropertyUpdaterService,
    IMapper Mapper,
    IMemberService MemberService,
    IConfigurationItemValidator ConfigurationItemValidator,
    IFileUploadService FileUploadService,
    ICartSharingService CartSharingService,
    ICartValidationContextFactory CartValidationContextFactory);
