using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class SaveForLaterItemsCommandHandler : CartCommandHandler<SaveForLaterItemsCommand>
    {
        protected const string savedForLaterCartType = "SavedForLater";

        public SaveForLaterItemsCommandHandler(ICartAggregateRepository cartAggregateRepository)
            : base(cartAggregateRepository)
        {
        }

        public override async Task<CartAggregate> Handle(SaveForLaterItemsCommand request, CancellationToken cancellationToken)
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

            return saveForLaterList;//or cart? or both? ask teams
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

            return await CartRepository.GetCartAsync(saveForLaterListSearchCriteria, request.CultureName);
        }

        protected virtual async Task<CartAggregate> CreateSaveForLaterListAsync(SaveForLaterItemsCommand request)
        {
            var createRequest = AbstractTypeFactory<SaveForLaterItemsCommand>.TryCreateInstance();
            createRequest.CopyFrom(request);
            createRequest.CartType = savedForLaterCartType;

            return await CreateNewCartAggregateAsync(createRequest);
        }
    }
}
