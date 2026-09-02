using System.Collections.Generic;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PricingModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.ShippingModule.Core.Model;
using VirtoCommerce.TaxModule.Core.Model;
using VirtoCommerce.XCart.Core.Models;
using CartAddress = VirtoCommerce.CartModule.Core.Model.Address;
using TaxAddress = VirtoCommerce.TaxModule.Core.Model.Address;

namespace VirtoCommerce.XCart.Core.Services;

public interface IXCartMapper
{
    TaxAddress ToTaxAddress(CartAddress source);

    GiftItem ToGiftItem(GiftReward source);

    GiftLineItem ToGiftLineItem(GiftItem source);

    /// <exception cref="System.ArgumentNullException"><paramref name="context"/> or its <see cref="CartMappingContext.NewCartItem"/> is null.</exception>
    LineItem ToLineItem(CartProduct source, CartMappingContext context);

    IEnumerable<TaxLine> ToTaxLines(ShippingRate source);

    IEnumerable<TaxLine> ToTaxLines(PaymentMethod source);

    ProductPromoEntry ToProductPromoEntry(LineItem source);

    PriceEvaluationContext ToPriceEvaluationContext(CartAggregate source);

    PriceEvaluationContext ToPriceEvaluationContext(CartProductsRequest source);

    TaxEvaluationContext ToTaxEvaluationContext(CartAggregate source);

    void MapTo(CartAggregate source, PromotionEvaluationContext target);

    void MapTo(CartAggregate source, TaxEvaluationContext target);

    void MapTo(IList<IFilter> filters, ShoppingCartSearchCriteria criteria);
}
