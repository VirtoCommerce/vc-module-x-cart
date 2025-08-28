using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Services;

public class CartSharingService(ICartAggregateRepository cartAggregateRepository) : ICartSharingService
{
    public virtual string GetSharingScope(ShoppingCart cart)
    {
        if (cart == null)
        {
            return CartSharingScope.Private;
        }

        if (cart.SharingSettings.IsNullOrEmpty())
        {
            return string.IsNullOrEmpty(cart.OrganizationId) ? CartSharingScope.Private : CartSharingScope.Organization;
        }

        if (cart.SharingSettings.Any(x => x.Scope == CartSharingScope.AnyoneAnonymous))
        {
            return CartSharingScope.AnyoneAnonymous;
        }
        else if (cart.SharingSettings.Any(x => x.Scope == CartSharingScope.AnyoneAuthorized))
        {
            return CartSharingScope.AnyoneAuthorized;
        }
        else if (cart.SharingSettings.Any(x => x.Scope == CartSharingScope.Organization))
        {
            return CartSharingScope.Organization;
        }
        else if (cart.SharingSettings.Any(x => x.Scope == CartSharingScope.User))
        {
            return CartSharingScope.User;
        }

        return CartSharingScope.Private;
    }

    public virtual string GetSharingAccess(ShoppingCart cart, string currentUserId)
    {
        var sharingScope = GetSharingScope(cart);

        if (sharingScope == CartSharingScope.Private || sharingScope == CartSharingScope.Organization)
        {
            return CartSharingAccess.Write;
        }
        else if (sharingScope == CartSharingScope.AnyoneAnonymous || sharingScope == CartSharingScope.AnyoneAuthorized)
        {
            return !string.IsNullOrEmpty(currentUserId) && GetSharingOwnerUserId(cart) == currentUserId ? CartSharingAccess.Write : CartSharingAccess.Read;
        }
        else
        {
            return CartSharingAccess.Read;
        }
    }

    public virtual bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId)
    {
        if (cart.SharingSettings.Any(x => x.Scope == CartSharingScope.AnyoneAnonymous))
        {
            return true;
        }
        else if (cart.SharingSettings.Any(x => x.Scope == CartSharingScope.AnyoneAuthorized))
        {
            return !string.IsNullOrEmpty(currentUserId);
        }
        else if (cart.SharingSettings.Any(x => x.Scope == CartSharingScope.Organization))
        {
            return !string.IsNullOrEmpty(currentUserId) && GetSharingOwnerOrganizationId(cart) == currentOrganizationId;
        }

        return !string.IsNullOrEmpty(currentUserId) && GetSharingOwnerUserId(cart) == currentUserId;
    }

    public virtual void SetOwner(ShoppingCart cart, string userId, string customerName, string organizationId)
    {
        cart.CustomerId = userId;
        cart.CustomerName = customerName;
        cart.OrganizationId = organizationId;
    }

    public virtual string GetSharingOwnerUserId(ShoppingCart cart)
    {
        return cart.CustomerId;
    }

    public virtual string GetSharingOwnerOrganizationId(ShoppingCart cart)
    {
        return cart.OrganizationId;
    }

    public virtual void EnsureSharingSettings(ShoppingCart cart, string sharingKey, string mode, string access)
    {
        if (cart.SharingSettings.IsNullOrEmpty())
        {
            var sharingSetting = AbstractTypeFactory<CartSharingSetting>.TryCreateInstance();

            sharingSetting.Id = sharingKey;
            sharingSetting.ShoppingCartId = cart.Id;
            sharingSetting.Scope = mode;
            sharingSetting.Access = access;

            cart.SharingSettings.Add(sharingSetting);
        }
        else
        {
            foreach (var setting in cart.SharingSettings)
            {
                setting.Scope = CartSharingScope.Private;
            }

            var sharingSetting = cart.SharingSettings.First();

            sharingSetting.Scope = mode;
            sharingSetting.Access = access;
        }
    }

    public virtual async Task<CartAggregate> GetWishlistBySharingKeyAsync(string sharingKey, IList<string> includeFields)
    {
        var cartSearchCriteria = AbstractTypeFactory<ShoppingCartSearchCriteria>.TryCreateInstance();

        cartSearchCriteria.SharingKey = sharingKey;
        cartSearchCriteria.Take = 1;

        var searchResult = await cartAggregateRepository.SearchCartAsync(cartSearchCriteria, includeFields);
        return searchResult.Results.FirstOrDefault();
    }
}
