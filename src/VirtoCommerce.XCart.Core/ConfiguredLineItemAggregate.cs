using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.MarketingModule.Core.Services;
using VirtoCommerce.TaxModule.Core.Services;
using VirtoCommerce.Xapi.Core.Pipelines;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Core
{
    public class ConfiguredLineItemAggregate : CartAggregate
    {
        private readonly IMarketingPromoEvaluator _marketingEvaluator;
        private readonly IShoppingCartTotalsCalculator _cartTotalsCalculator;
        private readonly ITaxProviderSearchService _taxProviderSearchService;
        private readonly ICartProductService _cartProductService;
        private readonly IDynamicPropertyUpdaterService _dynamicPropertyUpdaterService;
        private readonly IMemberService _memberService;
        private readonly IMapper _mapper;
        private readonly IGenericPipelineLauncher _pipeline;

        public ConfiguredLineItemAggregate(
            IMarketingPromoEvaluator marketingEvaluator,
            IShoppingCartTotalsCalculator cartTotalsCalculator,
            ITaxProviderSearchService taxProviderSearchService,
            ICartProductService cartProductService,
            IDynamicPropertyUpdaterService dynamicPropertyUpdaterService,
            IMapper mapper,
            IMemberService memberService,
            IGenericPipelineLauncher pipeline)
            : base(marketingEvaluator, cartTotalsCalculator, taxProviderSearchService, cartProductService, dynamicPropertyUpdaterService, mapper, memberService, pipeline)
        {
            _cartTotalsCalculator = cartTotalsCalculator;
            _marketingEvaluator = marketingEvaluator;
            _taxProviderSearchService = taxProviderSearchService;
            _cartProductService = cartProductService;
            _dynamicPropertyUpdaterService = dynamicPropertyUpdaterService;
            _mapper = mapper;
            _memberService = memberService;
            _pipeline = pipeline;
        }

        public async Task InitializeAsync(CreateConfiguredLineItemCommand command)
        {
            // create line items by configuration sections
            ValidationRuleSet = ["default"];
            var lineItems = command.ConfigurationSections
                .Where(x => x.Value != null)
                .Select(section => new NewCartItem(section.Value.ProductId, section.Value.Quantity))
                .ToList();

            await AddItemsAsync(lineItems);

            await RecalculateAsync();
        }
    }
}
