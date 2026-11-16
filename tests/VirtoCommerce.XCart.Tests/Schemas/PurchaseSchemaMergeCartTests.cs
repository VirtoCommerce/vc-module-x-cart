using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GraphQL;
using GraphQL.Execution;
using GraphQL.Types;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.DistributedLock;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Security.Authorization;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Schemas;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Authorization;
using VirtoCommerce.XCart.Data.Schemas;
using VirtoCommerce.XCart.Tests.Helpers;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Schemas
{
    /// <summary>
    /// mergeCart copies the second cart's items into the caller's cart and then deletes the second cart. Only the
    /// caller's own cart used to be authorized, so the id of anyone's cart was enough to read and delete it (VCST-6185).
    /// </summary>
    public class PurchaseSchemaMergeCartTests : XCartMoqHelper
    {
        private const string CallerId = "caller-user";
        private const string CallerCartId = "caller-cart";
        private const string SecondCartId = "second-cart";

        // GetUserId reads the claim types the platform registers at startup; nothing else in this suite sets them.
        static PurchaseSchemaMergeCartTests()
        {
            ClaimsPrincipalExtensions.UserIdClaimTypes = [ClaimTypes.NameIdentifier];
        }

        [Fact]
        public async Task CartOfAnotherUser_IsRefused()
        {
            var act = () => MergeAsync(new ShoppingCart { Id = SecondCartId, StoreId = DEFAULT_STORE_ID, CustomerId = "other-user" });

            await act.Should().ThrowAsync<AuthorizationError>();
            VerifyMerged(Times.Never());
        }

        [Fact]
        public async Task OwnCart_IsMerged()
        {
            // The control: the same merge, of a cart the caller owns.
            await MergeAsync(new ShoppingCart { Id = SecondCartId, StoreId = DEFAULT_STORE_ID, CustomerId = CallerId });

            VerifyMerged(Times.Once());
        }

        [Fact]
        public async Task GuestCart_IsMerged()
        {
            // Sign-in merges the cart the user filled as a guest. Its customer is the guest id, never the user's, so the
            // plain cart-access check would refuse it and break that merge.
            await MergeAsync(new ShoppingCart { Id = SecondCartId, StoreId = DEFAULT_STORE_ID, CustomerId = "guest-id", IsAnonymous = true });

            VerifyMerged(Times.Once());
        }

        [Fact]
        public async Task GuestCartOfAnotherStore_IsRefused()
        {
            // Anyone may merge a guest cart, but only within the store the merge runs in.
            var act = () => MergeAsync(new ShoppingCart { Id = SecondCartId, StoreId = "other-store", CustomerId = "guest-id", IsAnonymous = true });

            await act.Should().ThrowAsync<AuthorizationError>();
            VerifyMerged(Times.Never());
        }

        private async Task MergeAsync(ShoppingCart secondCart)
        {
            var carts = new List<ShoppingCart>
            {
                new() { Id = CallerCartId, StoreId = DEFAULT_STORE_ID, CustomerId = CallerId },
                secondCart,
            };

            var cartService = new Mock<IShoppingCartService>();
            // GetByIdAsync is a CRUD extension over GetAsync, so the mock has to sit on the interface method.
            cartService
                .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync((IList<string> ids, string _, bool _) => carts.Where(x => ids.Contains(x.Id)).ToList());

            var distributedLock = new Mock<IDistributedLock>();
            distributedLock
                .Setup(x => x.AcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<IDistributedLockHandle>());

            _mediatorMock
                .Setup(x => x.Send(It.IsAny<MergeCartCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GetValidCartAggregate());

            var purchaseSchema = new PurchaseSchema(
                CartAccessAuthorizationService(),
                cartService.Object,
                Mock.Of<IShoppingCartSearchService>(),
                distributedLock.Object,
                Mock.Of<IUserManagerCore>(),
                Mock.Of<IMemberResolver>(),
                Mock.Of<ICartSharingService>());

            var schema = new Schema { Query = new ObjectGraphType(), Mutation = new ObjectGraphType() };
            purchaseSchema.Build(schema);
            var field = schema.Mutation.Fields.Find("mergeCart");
            field.Arguments.Find("command").ResolvedType = InitializedCommandType();

            var services = new Mock<IServiceProvider>();
            services.Setup(x => x.GetService(typeof(IMediator))).Returns(_mediatorMock.Object);

            var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, CallerId)], "Test"));

            // A parsed input object, as GraphQL hands it over: the cart lock reads userId off it as a dictionary.
            var command = new Dictionary<string, object>
            {
                ["cartId"] = CallerCartId,
                ["storeId"] = DEFAULT_STORE_ID,
                ["userId"] = CallerId,
                ["secondCartId"] = SecondCartId,
            };

            var context = new Mock<IResolveFieldContext>();
            context.SetupGet(x => x.Arguments).Returns(new Dictionary<string, ArgumentValue>
            {
                ["command"] = new ArgumentValue(command, ArgumentSource.Literal),
            });
            context.SetupGet(x => x.User).Returns(principal);
            context.SetupGet(x => x.RequestServices).Returns(services.Object);
            // The X-API reads the caller off the user context, not off IResolveFieldContext.User.
            context.SetupGet(x => x.UserContext).Returns(new GraphQLUserContext(principal));
            context.SetupGet(x => x.FieldDefinition).Returns(field);

            await field.Resolver.ResolveAsync(context.Object);
        }

        // Converting the input object into the command needs the argument's graph type as schema initialization
        // resolves it; the purchase schema cannot be initialized here, so a one-field schema resolves it instead.
        private static IGraphType InitializedCommandType()
        {
            var query = new ObjectGraphType { Name = "Query" };
            query.Field<StringGraphType>("probe").Argument<NonNullGraphType<InputMergeCartType>>("command");
            new Schema { Query = query }.Initialize();

            return query.Fields.Find("probe").Arguments.Find("command").ResolvedType;
        }

        // The real cart-access rules decide; only the platform's authorization plumbing is stubbed.
        private static IAuthorizationService CartAccessAuthorizationService()
        {
            var handler = new CanAccessCartAuthorizationHandler(() => null, Mock.Of<IShoppingCartService>(), Mock.Of<ICartSharingService>());

            var authorizationService = new Mock<IAuthorizationService>();
            authorizationService
                .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
                .Returns(async (ClaimsPrincipal user, object resource, IEnumerable<IAuthorizationRequirement> requirements) =>
                {
                    var authorizationContext = new AuthorizationHandlerContext(requirements, user, resource);
                    await handler.HandleAsync(authorizationContext);

                    return authorizationContext.HasSucceeded ? AuthorizationResult.Success() : AuthorizationResult.Failed();
                });

            return authorizationService.Object;
        }

        private void VerifyMerged(Times times)
        {
            _mediatorMock.Verify(x => x.Send(It.IsAny<MergeCartCommand>(), It.IsAny<CancellationToken>()), times);
        }
    }
}
