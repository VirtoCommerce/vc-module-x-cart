using System;
using System.Collections.Generic;
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
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.DistributedLock;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Security.Authorization;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Schemas;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Schemas
{
    /// <summary>
    /// The handler behind a wishlist mutation persists, so a resolver that sends first and authorizes afterwards
    /// lets anyone who knows a list id write to someone else's list and only then be told no (VCST-6116). The
    /// ordering is held by hand in every mutation resolver and has drifted once, so it is pinned here.
    /// </summary>
    public class PurchaseSchemaAuthorizationOrderTests
    {
        private const string ListId = "list-1";

        // GetUserId reads the claim types the platform registers at startup; nothing else in this suite sets them.
        static PurchaseSchemaAuthorizationOrderTests()
        {
            ClaimsPrincipalExtensions.UserIdClaimTypes = [ClaimTypes.NameIdentifier];
        }

        [Theory]
        [InlineData("addWishlistItem")]
        [InlineData("updateWishListItems")]
        [InlineData("addWishlistItems")]
        [InlineData("removeWishlistItem")]
        [InlineData("moveWishlistItem")]
        [InlineData("changeWishlist")]
        [InlineData("removeWishlist")]
        public async Task RefusedMutation_NeverReachesTheHandler(string fieldName)
        {
            var harness = new Harness();

            var act = () => harness.ResolveAsync(fieldName);

            await act.Should().ThrowAsync<AuthorizationError>();
            harness.Mediator.Verify(x => x.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private sealed class Harness
        {
            public Mock<IMediator> Mediator { get; } = new();

            private readonly ISchema _schema;

            public Harness()
            {
                var authorizationService = new Mock<IAuthorizationService>();
                authorizationService
                    .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
                    .ReturnsAsync(AuthorizationResult.Failed());

                var cartService = new Mock<IShoppingCartService>();
                // GetByIdAsync is a CRUD extension over GetAsync, so the mock has to sit on the interface method.
                cartService
                    .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
                    .ReturnsAsync([new ShoppingCart { Id = ListId, CustomerId = "owner-user" }]);

                var memberResolver = new Mock<IMemberResolver>();
                memberResolver
                    .Setup(x => x.ResolveMemberByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync((Member)null);

                var userManagerCore = new Mock<IUserManagerCore>();

                var purchaseSchema = new PurchaseSchema(
                    authorizationService.Object,
                    cartService.Object,
                    Mock.Of<IShoppingCartSearchService>(),
                    Mock.Of<IDistributedLock>(),
                    userManagerCore.Object,
                    memberResolver.Object,
                    CartSharingScopeFixturesService());

                _schema = new Schema { Query = new ObjectGraphType(), Mutation = new ObjectGraphType() };
                purchaseSchema.Build(_schema);
            }

            public async Task<object> ResolveAsync(string fieldName)
            {
                var field = _schema.Mutation.Fields.Find(fieldName);
                field.Should().NotBeNull($"{fieldName} must exist on the mutation type");

                var services = new Mock<IServiceProvider>();
                services.Setup(x => x.GetService(typeof(IMediator))).Returns(Mediator.Object);

                var context = new Mock<IResolveFieldContext>();
                context.SetupGet(x => x.Arguments).Returns(new Dictionary<string, ArgumentValue>
                {
                    ["command"] = new ArgumentValue(CommandFor(fieldName), ArgumentSource.Literal),
                });
                var principal = Principal();

                context.SetupGet(x => x.User).Returns(principal);
                context.SetupGet(x => x.RequestServices).Returns(services.Object);
                // The X-API reads the caller off the user context, not off IResolveFieldContext.User.
                context.SetupGet(x => x.UserContext).Returns(new GraphQLUserContext(principal));
                context.SetupGet(x => x.FieldDefinition).Returns(field);

                return await field.Resolver.ResolveAsync(context.Object);
            }

            // Already materialized, so no argument graph type has to be resolved to read it back.
            private static object CommandFor(string fieldName) => fieldName switch
            {
                "addWishlistItem" => new AddWishlistItemCommand(ListId, "product-1"),
                "updateWishListItems" => new UpdateWishlistItemsCommand { ListId = ListId },
                "addWishlistItems" => new AddWishlistItemsCommand { ListId = ListId },
                "removeWishlistItem" => new RemoveWishlistItemCommand(ListId, "line-1", "product-1"),
                "moveWishlistItem" => new MoveWishlistItemCommand(ListId, "list-2", "line-1"),
                "changeWishlist" => new ChangeWishlistCommand { ListId = ListId },
                "removeWishlist" => new RemoveWishlistCommand(ListId),
                _ => throw new ArgumentOutOfRangeException(nameof(fieldName), fieldName, "No command mapped."),
            };

            private static ClaimsPrincipal Principal()
            {
                var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "other-user")], "Test");

                return new ClaimsPrincipal(identity);
            }

            private static ICartSharingService CartSharingScopeFixturesService()
            {
                var sharingService = new Mock<ICartSharingService>();
                sharingService.Setup(x => x.GetSharingScope(It.IsAny<ShoppingCart>())).Returns(CartSharingScope.Private);
                sharingService
                    .Setup(x => x.ConfigureSearchCriteria(It.IsAny<ShoppingCartSearchCriteria>(), It.IsAny<string>()));

                return sharingService.Object;
            }
        }
    }
}
