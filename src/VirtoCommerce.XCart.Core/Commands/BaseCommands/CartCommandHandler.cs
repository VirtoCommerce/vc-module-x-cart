using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Tax;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Core.Commands.BaseCommands
{
    public abstract class CartCommandHandler<TCartCommand> : IRequestHandler<TCartCommand, CartAggregate> where TCartCommand : CartCommand
    {
        protected CartCommandHandler(ICartAggregateRepository cartAggregateRepository)
        {
            CartRepository = cartAggregateRepository;
        }

        protected ICartAggregateRepository CartRepository { get; private set; }

        public abstract Task<CartAggregate> Handle(TCartCommand request, CancellationToken cancellationToken);

        //TODO: Make obsolete and move to ICartAggregateRepository? 
        protected virtual async Task<CartAggregate> GetOrCreateCartFromCommandAsync(TCartCommand request)
        {
            return await CartRepository.EnsureUserCartAsync(request);
        }

        //TODO: Make obsolete and move to ICartAggregateRepository? 
        protected virtual ShoppingCartSearchCriteria GetCartSearchCriteria(TCartCommand request)
        {
            var cartSearchCriteria = AbstractTypeFactory<ShoppingCartSearchCriteria>.TryCreateInstance();

            cartSearchCriteria.Name = request.CartName;
            cartSearchCriteria.StoreId = request.StoreId;
            cartSearchCriteria.CustomerId = request.UserId;
            cartSearchCriteria.OrganizationId = request.OrganizationId;
            cartSearchCriteria.OrganizationIdIsEmpty = string.IsNullOrEmpty(request.OrganizationId);
            cartSearchCriteria.Currency = request.CurrencyCode;
            cartSearchCriteria.Type = request.CartType;

            return cartSearchCriteria;
        }

        //TODO: Make obsolete and move to ICartAggregateRepository? 
        protected virtual Task<CartAggregate> GetCartById(string cartId, string language) => CartRepository.GetCartByIdAsync(cartId, language);

        //TODO: Make obsolete and move to ICartAggregateRepository? 
        protected virtual Task<CartAggregate> GetCart(ShoppingCartSearchCriteria cartSearchCriteria, string language) => CartRepository.GetCartAsync(cartSearchCriteria, language);

        //TODO: Make obsolete and move to ICartAggregateRepository? 
        protected virtual Task<CartAggregate> CreateNewCartAggregateAsync(TCartCommand request)
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
            cart.SharingSettings = new List<CartSharingSetting>();

            return CartRepository.GetCartForShoppingCartAsync(cart);
        }

        //TODO: Make obsolete and move to ICartAggregateRepository? 
        protected virtual async Task<CartAggregate> SaveCartAsync(CartAggregate cartAggregate)
        {
            await CartRepository.SaveAsync(cartAggregate);
            return cartAggregate;
        }
    }
}
