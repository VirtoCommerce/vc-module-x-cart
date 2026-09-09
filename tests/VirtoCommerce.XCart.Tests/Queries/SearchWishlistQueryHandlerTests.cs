using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Queries;
using VirtoCommerce.XCart.Tests.Helpers;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Queries
{
    // The handler is the only WithScope caller - asserts the criteria it hands the repository.
    public class SearchWishlistQueryHandlerTests
    {
        private const string UserId = "customer-1";
        private const string OrgId = "org-1";

        [Theory]
        [InlineData(CartSharingScope.Organization, null, OrgId)]
        [InlineData(CartSharingScope.Private, UserId, null)]
        [InlineData(null, UserId, OrgId)]
        public async Task Handle_NarrowsTheCriteriaForTheRequestedScope(string scope, string expectedCustomerId, string expectedOrganizationId)
        {
            var repository = new Mock<ICartAggregateRepository>();
            ShoppingCartSearchCriteria captured = null;

            repository
                .Setup(x => x.SearchCartAsync(It.IsAny<ShoppingCartSearchCriteria>(), It.IsAny<IList<string>>()))
                .Callback<ShoppingCartSearchCriteria, IList<string>>((criteria, _) => captured = criteria)
                .ReturnsAsync(new SearchCartResponse());

            var handler = new SearchWishlistQueryHandler(
                repository.Object,
                Mock.Of<ISearchPhraseParser>(),
                Mock.Of<ISavedForLaterListService>(),
                Mock.Of<IXCartMapper>(),
                CartSharingScopeFixtures.SharingService());

            var request = new SearchWishlistQuery
            {
                StoreId = "B2B-store",
                CurrencyCode = "USD",
                CultureName = "en-US",
                UserId = UserId,
                OrganizationId = OrgId,
                Scope = scope,
                Take = 20,
            };

            await handler.Handle(request, CancellationToken.None);

            captured.Should().NotBeNull();
            captured.CustomerId.Should().Be(expectedCustomerId);
            captured.OrganizationId.Should().Be(expectedOrganizationId);
        }
    }
}
