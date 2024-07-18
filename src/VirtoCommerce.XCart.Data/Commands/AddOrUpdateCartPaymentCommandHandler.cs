using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class AddOrUpdateCartPaymentCommandHandler : CartCommandHandler<AddOrUpdateCartPaymentCommand>
    {
        private readonly ICartAvailMethodsService _cartAvailMethodService;

        public AddOrUpdateCartPaymentCommandHandler(ICartAggregateRepository cartAggregateRepository, ICartAvailMethodsService cartAvailMethodService)
            : base(cartAggregateRepository)
        {
            _cartAvailMethodService = cartAvailMethodService;
        }

        public override async Task<CartAggregate> Handle(AddOrUpdateCartPaymentCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

            var paymentId = request.Payment.Id?.Value ?? null;
            var payment = cartAggregate.Cart.Payments.FirstOrDefault(s => paymentId != null && s.Id == paymentId);
            payment = request.Payment.MapTo(payment);

            await cartAggregate.AddPaymentAsync(payment, await _cartAvailMethodService.GetAvailablePaymentMethodsAsync(cartAggregate));

            if (!request.Payment.DynamicProperties.IsNullOrEmpty())
            {
                await cartAggregate.UpdateCartPaymentDynamicProperties(payment, request.Payment.DynamicProperties);
            }

            cartAggregate = await SaveCartAsync(cartAggregate);
            return await GetCartById(cartAggregate.Cart.Id, request.CultureName);
        }
    }
}
