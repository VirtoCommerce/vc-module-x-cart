using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Core.Commands.BaseCommands
{
    /// <summary>
    /// Base handler for configuration-item mutations. Enriches the request's configuration sections with their
    /// catalog names (a single, guaranteed step for every subclass) before delegating the actual aggregate
    /// mutation to <see cref="ApplyConfigurationAsync"/>.
    /// </summary>
    public abstract class CartConfigurationCommandHandler<TCommand> : CartCommandHandler<TCommand>
        where TCommand : CartCommand, ICartConfigurationCommand
    {
        private readonly ICartConfigurationService _cartConfigurationService;

        protected CartConfigurationCommandHandler(
            ICartAggregateRepository cartAggregateRepository,
            ICartConfigurationService cartConfigurationService)
            : base(cartAggregateRepository)
        {
            _cartConfigurationService = cartConfigurationService;
        }

        public sealed override async Task<CartAggregate> Handle(TCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

            var lineItem = cartAggregate.GetConfiguredLineItem(request.LineItemId);
            if (lineItem is not null)
            {
                await _cartConfigurationService.UpdateSectionsFromCatalogAsync(lineItem.ProductId, request.ConfigurationSections);
            }

            await ApplyConfigurationAsync(cartAggregate, request);

            return await SaveCartAsync(cartAggregate);
        }

        protected abstract Task ApplyConfigurationAsync(CartAggregate cartAggregate, TCommand request);
    }
}
