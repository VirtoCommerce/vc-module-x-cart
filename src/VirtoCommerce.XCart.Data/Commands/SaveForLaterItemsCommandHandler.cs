using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Tax;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class SaveForLaterItemsCommandHandler(ICartAggregateRepository cartAggregateRepository) : IRequestHandler<SaveForLaterItemsCommand, CartAggregateWithList>
    {
        protected const string savedForLaterCartType = "SavedForLater";

        public async Task<CartAggregateWithList> Handle(SaveForLaterItemsCommand request, CancellationToken cancellationToken)
        {
            var cart = await GetCartById(request.CartId, request.CultureName);

            if (cart == null)
            {
                throw new OperationCanceledException("Cart not found");
            }

            var saveForLaterList = await GetSaveForLaterListAsync(request);

            foreach (var lineItemId in request.LineItemIds)
            {
                var item = cart.Cart.Items.FirstOrDefault(x => x.Id == lineItemId);

                if (item != null)
                {
                    saveForLaterList = await saveForLaterList.AddItemsAsync(new List<NewCartItem> { new NewCartItem(item.ProductId, item.Quantity) });
                    await cart.RemoveItemAsync(lineItemId);
                }
            }

            await SaveCartAsync(saveForLaterList);
            await SaveCartAsync(cart);

            return new CartAggregateWithList() { Cart = cart, List = saveForLaterList };
        }

        protected virtual async Task<CartAggregate> GetSaveForLaterListAsync(SaveForLaterItemsCommand request)
        {
            var existingSaveForLaterList = await FindExistingSaveForLaterListAsync(request);

            if (existingSaveForLaterList != null)
            {
                return existingSaveForLaterList;
            }

            return await CreateSaveForLaterListAsync(request);
        }

        protected virtual async Task<CartAggregate> FindExistingSaveForLaterListAsync(SaveForLaterItemsCommand request)
        {
            var saveForLaterListSearchCriteria = AbstractTypeFactory<ShoppingCartSearchCriteria>.TryCreateInstance();
            saveForLaterListSearchCriteria.Type = savedForLaterCartType;
            saveForLaterListSearchCriteria.CustomerId = request.UserId;
            saveForLaterListSearchCriteria.OrganizationId = request.OrganizationId;
            saveForLaterListSearchCriteria.Currency = request.CurrencyCode;//Do we need Currency here?

            return await cartAggregateRepository.GetCartAsync(saveForLaterListSearchCriteria, request.CultureName);
        }

        protected virtual async Task<CartAggregate> CreateSaveForLaterListAsync(SaveForLaterItemsCommand request)
        {
            var createRequest = AbstractTypeFactory<SaveForLaterItemsCommand>.TryCreateInstance();
            createRequest.CopyFrom(request);
            createRequest.CartType = savedForLaterCartType;

            return await CreateNewCartAggregateAsync(createRequest);
        }

        protected virtual Task<CartAggregate> GetCartById(string cartId, string language) => cartAggregateRepository.GetCartByIdAsync(cartId, language);

        protected virtual Task SaveCartAsync(CartAggregate cartAggregate) => cartAggregateRepository.SaveAsync(cartAggregate);

        protected virtual Task<CartAggregate> CreateNewCartAggregateAsync(SaveForLaterItemsCommand request)
        {
            var cart = AbstractTypeFactory<ShoppingCart>.TryCreateInstance();

            cart.CustomerId = request.UserId;
            cart.OrganizationId = request.OrganizationId;
            cart.Name = request.CartName ?? "default";
            cart.StoreId = request.StoreId;
            cart.LanguageCode = request.CultureName;
            cart.Type = request.CartType;
            cart.Currency = request.CurrencyCode;
            cart.Items = new List<LineItem>();
            cart.Shipments = new List<Shipment>();
            cart.Payments = new List<Payment>();
            cart.Addresses = new List<CartModule.Core.Model.Address>();
            cart.TaxDetails = new List<TaxDetail>();
            cart.Coupons = new List<string>();
            cart.Discounts = new List<Discount>();
            cart.DynamicProperties = new List<DynamicObjectProperty>();

            return cartAggregateRepository.GetCartForShoppingCartAsync(cart);
        }
    }
}
