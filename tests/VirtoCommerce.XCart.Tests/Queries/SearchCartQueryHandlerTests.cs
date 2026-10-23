using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Queries;
using VirtoCommerce.XCart.Data.Services;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Queries
{
    public class SearchCartQueryHandlerTests
    {
        private const string UserId = "customer-1";
        private const string OrgId = "org-1";
        private const string OtherUserId = "customer-2";
        private const string OtherOrgId = "org-2";

        [Theory]
        [InlineData(OrgId)]
        [InlineData(null)]
        public async Task Handle_FilterWithOwners_OwnerNotOvewrittenByFilters(string organizationId)
        {
            var criteria = await SearchAsync(organizationId,
                new TermFilter { FieldName = "customerOrOrganization", Values = ["true"] },
                new TermFilter { FieldName = "customerId", Values = [OtherUserId] },
                new TermFilter { FieldName = "customerIds", Values = [OtherUserId] },
                new TermFilter { FieldName = "organizationId", Values = [OtherOrgId] });

            criteria.CustomerOrOrganization.Should().BeFalse();
            criteria.CustomerIds.Should().BeNull();
            criteria.CustomerId.Should().Be(UserId);
            criteria.OrganizationId.Should().Be(organizationId);
        }

        [Fact]
        public async Task Handle_FilterWithOwners_OwnerPresentInSearchCriteria()
        {
            var criteria = await SearchAsync(OrgId,
                new TermFilter { FieldName = "name", Values = ["Default"] },
                new TermFilter { FieldName = "status", Values = ["New"] });

            criteria.Name.Should().Be("Default");
            criteria.Status.Should().Be("New");
            criteria.CustomerId.Should().Be(UserId);
            criteria.OrganizationId.Should().Be(OrgId);
        }

        private static async Task<ShoppingCartSearchCriteria> SearchAsync(string organizationId, params IFilter[] filters)
        {
            var filter = "will be parsed by the mock";

            var phraseParser = new Mock<ISearchPhraseParser>();
            phraseParser
                .Setup(x => x.Parse(filter))
                .Returns(new SearchPhraseParseResult { Filters = [.. filters] });

            var repository = new Mock<ICartAggregateRepository>();
            ShoppingCartSearchCriteria capturedCriteria = null;

            repository
                .Setup(x => x.SearchCartAsync(It.IsAny<ShoppingCartSearchCriteria>()))
                .Callback<ShoppingCartSearchCriteria>(criteria => capturedCriteria = criteria)
                .ReturnsAsync(new SearchCartResponse());

            var handler = new SearchCartQueryHandler(
                repository.Object,
                phraseParser.Object,
                Mock.Of<ICartResponseGroupParser>(),
                new XCartMapper(new CartItemBuilder()));

            var request = new SearchCartQuery
            {
                StoreId = "B2B-store",
                UserId = UserId,
                OrganizationId = organizationId,
                Filter = filter,
                Take = 20,
            };

            await handler.Handle(request, CancellationToken.None);

            capturedCriteria.Should().NotBeNull();
            return capturedCriteria;
        }
    }
}
