using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Schemas;

namespace VirtoCommerce.XCart.Data.Queries;

public class GetPricesSumQueryBuilder : QueryBuilder<GetPricesSumQuery, ExpPricesSum, PricesSumType>
{
    public GetPricesSumQueryBuilder(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }

    protected override string Name => "pricesSum";
}
