using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class ChangeCartCurrencyCommandHandler : CartCommandHandler<ChangeCartCurrencyCommand>
    {
        public ChangeCartCurrencyCommandHandler(ICartAggregateRepository cartAggregateRepository)
            : base(cartAggregateRepository)
        {
        }

        public override async Task<CartAggregate> Handle(ChangeCartCurrencyCommand request, CancellationToken cancellationToken)
        {
            // get (or create) both carts
            var currentCurrencyCartAggregate = await GetOrCreateCartFromCommandAsync(request)
                ?? throw new OperationCanceledException("Cart not found");

            var newCurrencyCartRequest = new ChangeCartCurrencyCommand
            {
                StoreId = request.StoreId ?? currentCurrencyCartAggregate.Cart.StoreId,
                CartName = request.CartName ?? currentCurrencyCartAggregate.Cart.Name,
                CartType = request.CartType ?? currentCurrencyCartAggregate.Cart.Type,
                UserId = request.UserId ?? currentCurrencyCartAggregate.Cart.CustomerId,
                OrganizationId = request.OrganizationId ?? currentCurrencyCartAggregate.Cart.OrganizationId,
                CultureName = request.CultureName ?? currentCurrencyCartAggregate.Cart.LanguageCode,
                CurrencyCode = request.NewCurrencyCode,
            };

            var newCurrencyCartAggregate = await GetOrCreateCartFromCommandAsync(newCurrencyCartRequest);

            // clear (old) cart items and add items from the currency cart
            newCurrencyCartAggregate.Cart.Items.Clear();

            var newCartItems = currentCurrencyCartAggregate.LineItems
                .Select(x => new NewCartItem(x.ProductId, x.Quantity)
                {
                    IgnoreValidationErrors = true,
                    Comment = x.Note,
                    IsSelectedForCheckout = x.SelectedForCheckout,
                    DynamicProperties = x.DynamicProperties.SelectMany(x => x.Values.Select(y => new DynamicPropertyValue()
                    {
                        Name = x.Name,
                        Value = y.Value,
                        Locale = y.Locale,
                    })).ToArray(),
                })
                .ToArray();

            await newCurrencyCartAggregate.AddItemsAsync(newCartItems);

            await CartRepository.SaveAsync(newCurrencyCartAggregate);
            return newCurrencyCartAggregate;
        }
    }
}
