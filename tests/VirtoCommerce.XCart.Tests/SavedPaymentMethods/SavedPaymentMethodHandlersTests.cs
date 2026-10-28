using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GraphQL;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Model.Search;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.PaymentModule.Model.Requests;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Data.Commands;
using VirtoCommerce.XCart.Data.Queries;
using Xunit;

namespace VirtoCommerce.XCart.Tests.SavedPaymentMethods
{
    public class SavedPaymentMethodHandlersTests
    {
        private readonly Mock<IStoredPaymentMethodManager> _manager = new();

        [Fact]
        public async Task Add_PassesTheCardFormParametersForTheSignedInUser()
        {
            AddStoredPaymentMethodRequest sent = null;
            var card = new StoredPaymentMethod { Id = "card-1" };
            _manager.Setup(x => x.AddAsync(It.IsAny<AddStoredPaymentMethodRequest>()))
                .Callback<AddStoredPaymentMethodRequest>(x => sent = x)
                .ReturnsAsync(StoredPaymentMethodOperationResult.Success(card));

            var result = await new SavedPaymentMethodCommandHandler(_manager.Object).Handle(new AddSavedPaymentMethodCommand
            {
                StoreId = "store-1",
                PaymentMethodCode = "AuthorizeNetPaymentMethod",
                UserId = "user-1",
                SetDefault = true,
                Parameters = [new KeyValue { Key = "dataValue", Value = "nonce" }, new KeyValue { Key = "billingZip", Value = "98004" }],
            }, CancellationToken.None);

            result.Should().BeSameAs(card);
            sent.CustomerId.Should().Be("user-1");
            sent.StoreId.Should().Be("store-1");
            sent.PaymentMethodCode.Should().Be("AuthorizeNetPaymentMethod");
            sent.SetDefault.Should().BeTrue();
            sent.Parameters["dataValue"].Should().Be("nonce");
            sent.Parameters["billingZip"].Should().Be("98004");
        }

        [Fact]
        public async Task Add_RequiresAuthentication_IsReportedWithItsOwnCode()
        {
            _manager.Setup(x => x.AddAsync(It.IsAny<AddStoredPaymentMethodRequest>()))
                .ReturnsAsync(new StoredPaymentMethodOperationResult { Outcome = ChargeOutcome.RequiresAction, ErrorMessage = "Confirm the card with your bank." });

            var act = () => new SavedPaymentMethodCommandHandler(_manager.Object).Handle(new AddSavedPaymentMethodCommand { UserId = "user-1" }, CancellationToken.None);

            var error = (await act.Should().ThrowAsync<ExecutionError>()).Which;
            error.Code.Should().Be(SavedPaymentMethodCommandHandler.AuthenticationRequiredCode);
            error.Message.Should().Be("Confirm the card with your bank.");
        }

        [Fact]
        public async Task Remove_RefusedByTheManager_ShowsItsReason()
        {
            _manager.Setup(x => x.RemoveAsync("user-1", "card-1"))
                .ReturnsAsync(StoredPaymentMethodOperationResult.Fail("This card pays for 2 subscriptions."));

            var act = () => new SavedPaymentMethodCommandHandler(_manager.Object).Handle(new RemoveSavedPaymentMethodCommand { UserId = "user-1", Id = "card-1" }, CancellationToken.None);

            var error = (await act.Should().ThrowAsync<ExecutionError>()).Which;
            error.Code.Should().Be(SavedPaymentMethodCommandHandler.SavedPaymentMethodErrorCode);
            error.Message.Should().Be("This card pays for 2 subscriptions.");
        }

        [Fact]
        public async Task SetDefault_ActsForTheSignedInUser()
        {
            var card = new StoredPaymentMethod { Id = "card-1", IsDefault = true };
            _manager.Setup(x => x.SetDefaultAsync("user-1", "card-1")).ReturnsAsync(StoredPaymentMethodOperationResult.Success(card));

            var result = await new SavedPaymentMethodCommandHandler(_manager.Object).Handle(new SetDefaultSavedPaymentMethodCommand { UserId = "user-1", Id = "card-1" }, CancellationToken.None);

            result.Should().BeSameAs(card);
        }

        [Fact]
        public async Task SavedPaymentMethods_AreTheSignedInUsersCardsWithoutRevokedOnes()
        {
            StoredPaymentMethodSearchCriteria criteria = null;
            var searchService = new Mock<IStoredPaymentMethodSearchService>();
            searchService.Setup(x => x.SearchAsync(It.IsAny<StoredPaymentMethodSearchCriteria>(), It.IsAny<bool>()))
                .Callback<StoredPaymentMethodSearchCriteria, bool>((x, _) => criteria = x)
                .ReturnsAsync(new StoredPaymentMethodSearchResult { Results = [new StoredPaymentMethod { Id = "card-1" }] });

            var result = await new GetSavedPaymentMethodsQueryHandler(searchService.Object).Handle(new GetSavedPaymentMethodsQuery { StoreId = "store-1", UserId = "user-1" }, CancellationToken.None);

            result.Should().ContainSingle();
            criteria.CustomerId.Should().Be("user-1");
            criteria.StoreId.Should().Be("store-1");
            criteria.Statuses.Should().NotContain(StoredPaymentMethodStatus.Revoked);
        }

        [Fact]
        public async Task CardPaymentMethods_AreTheMethodsThatCanSaveCards_WithTheirFormParameters()
        {
            var cardMethod = new FormPaymentMethod("CardMethod", new Dictionary<string, string> { ["clientKey"] = "public-key" }) { Priority = 1 };
            var failingMethod = new FormPaymentMethod("FailingMethod", null) { Priority = 2, Fails = true };
            var paymentMethodsSearch = new Mock<IPaymentMethodsSearchService>();
            paymentMethodsSearch.Setup(x => x.SearchAsync(It.IsAny<PaymentMethodsSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new PaymentMethodsSearchResult { Results = [new PlainPaymentMethod("Invoice"), cardMethod, failingMethod] });

            var result = await new GetCardPaymentMethodsQueryHandler(paymentMethodsSearch.Object, NullLogger<GetCardPaymentMethodsQueryHandler>.Instance)
                .Handle(new GetCardPaymentMethodsQuery { StoreId = "store-1", UserId = "user-1" }, CancellationToken.None);

            result.Select(x => x.PaymentMethodCode).Should().Equal("CardMethod");
            result[0].PublicParameters["clientKey"].Should().Be("public-key");
            cardMethod.Requests.Should().ContainSingle().Which.StoreForFutureUse.Should().BeTrue();
        }

        private sealed class PlainPaymentMethod(string code) : PaymentMethod(code)
        {
            public override PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;
            public override PaymentMethodGroupType PaymentMethodGroupType => PaymentMethodGroupType.Manual;
        }

        private sealed class FormPaymentMethod(string code, Dictionary<string, string> publicParameters) : PaymentMethod(code), ISupportStoredCredentialFlow
        {
            public bool Fails { get; set; }
            public List<ProcessPaymentRequest> Requests { get; } = [];

            public override PaymentMethodType PaymentMethodType => PaymentMethodType.PreparedForm;
            public override PaymentMethodGroupType PaymentMethodGroupType => PaymentMethodGroupType.BankCard;

            public override Task<ProcessPaymentRequestResult> ProcessPaymentAsync(ProcessPaymentRequest request, CancellationToken cancellationToken = default)
            {
                Requests.Add(request);
                if (Fails)
                {
                    throw new System.InvalidOperationException("needs an order");
                }

                return Task.FromResult(new ProcessPaymentRequestResult { IsSuccess = true, PublicParameters = publicParameters });
            }
        }
    }
}
