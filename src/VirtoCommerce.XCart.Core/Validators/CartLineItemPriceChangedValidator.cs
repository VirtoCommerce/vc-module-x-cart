using FluentValidation;
using VirtoCommerce.CoreModule.Core.Currency;

namespace VirtoCommerce.XCart.Core.Validators
{

    /// <summary>
    /// Represents a cart lineitem price changed validator which transformed to cart warning.
    /// </summary>
    public class CartLineItemPriceChangedValidator : AbstractValidator<CartLineItemPriceChangedValidationContext>
    {
        public CartLineItemPriceChangedValidator()
        {
            RuleFor(x => x).Custom((lineItemContext, context) =>
            {
                var lineItem = lineItemContext.LineItem;

                var productPairKey = lineItemContext.CartAggregate != null ?
                    lineItemContext.CartAggregate.GetCartProductKey(lineItem) :
                    CartAggregate.FormatGetCartProductKey(lineItem.ProductId, lineItem.Currency);

                if (lineItemContext.CartProducts.TryGetValue(productPairKey, out var cartProduct) &&
                    cartProduct?.Price != null)
                {
                    var tierPrice = cartProduct.Price.GetTierPrice(lineItem.Quantity);
                    // The line item keeps the exact sale price, ActualPrice.Amount is rounded to the currency:
                    // compare both at the currency precision
                    var currentPrice = new Money(lineItem.SalePrice, tierPrice.ActualPrice.Currency).Amount;
                    if (HasPriceChanged(currentPrice, tierPrice.ActualPrice.Amount))
                    {
                        context.AddFailure(CartErrorDescriber.ProductPriceChangedError(lineItem, lineItem.SalePrice, lineItem.SalePriceWithTax, tierPrice.ActualPrice.Amount, tierPrice.ActualPriceWithTax.Amount));
                    }
                }
            });
        }

        /// <summary>
        /// Responsible for determining if a product's price has changed from its current value to a new value. 
        /// </summary>
        /// <param name="currentPrice"></param>
        /// <param name="newPrice"></param>
        /// <returns>Returns true if price has changed, false if price has not changed.</returns>
        protected virtual bool HasPriceChanged(decimal currentPrice, decimal newPrice)
        {
            return newPrice > 0 && currentPrice != newPrice;
        }
    }
}
