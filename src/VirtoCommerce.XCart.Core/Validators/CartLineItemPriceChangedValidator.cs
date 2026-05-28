using FluentValidation;

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
                    CartAggregate.GetCartProductKey(lineItem.ProductId, lineItem.Currency);

                if (lineItemContext.CartProducts.TryGetValue(productPairKey, out var cartProduct) &&
                    cartProduct?.Price != null)
                {
                    var tierPrice = cartProduct.Price.GetTierPrice(lineItem.Quantity);
                    if (HasPriceChanged(lineItem.SalePrice, tierPrice.ActualPrice.Amount))
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
