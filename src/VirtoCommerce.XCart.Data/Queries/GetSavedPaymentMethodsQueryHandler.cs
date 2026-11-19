using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Queries;

namespace VirtoCommerce.XCart.Data.Queries;

/// <summary>The signed-in customer's cards in the store, including ones that need updating or expired; revoked cards are left out.</summary>
public class GetSavedPaymentMethodsQueryHandler(IStoredPaymentMethodSearchService storedPaymentMethodSearchService)
    : IQueryHandler<GetSavedPaymentMethodsQuery, IList<StoredPaymentMethod>>
{
    private const int MaxCards = 100;

    public virtual async Task<IList<StoredPaymentMethod>> Handle(GetSavedPaymentMethodsQuery request, CancellationToken cancellationToken)
    {
        var criteria = AbstractTypeFactory<StoredPaymentMethodSearchCriteria>.TryCreateInstance();
        criteria.StoreId = request.StoreId;
        criteria.CustomerId = request.UserId;
        criteria.Statuses = [StoredPaymentMethodStatus.Active, StoredPaymentMethodStatus.NeedsUpdate, StoredPaymentMethodStatus.Expired];
        criteria.Take = MaxCards;

        return (await storedPaymentMethodSearchService.SearchAsync(criteria)).Results;
    }
}
