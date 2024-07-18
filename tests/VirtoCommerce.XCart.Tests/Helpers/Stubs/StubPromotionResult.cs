using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;

namespace VirtoCommerce.XCart.Tests.Helpers.Stubs
{
    public class StubPromotionResult : PromotionResult
    {
        public new ICollection<PromotionReward> Rewards => Enumerable.Empty<PromotionReward>().ToList();
    }
}
