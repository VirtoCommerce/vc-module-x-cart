using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GraphQL;
using GraphQL.Types;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Security.Authorization;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Authorization;
using VirtoCommerce.XCart.Data.Commands.BaseCommands;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Builders;

public class CartCommandBuilderTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IAuthorizationService> _authorizationServiceMock = new();
    private readonly Mock<IDistributedLockService> _lockServiceMock = new();
    private readonly Mock<ICartAggregateRepository> _cartRepositoryMock = new();

    private TestableCartCommandBuilder CreateBuilder()
    {
        return new TestableCartCommandBuilder(
            _mediatorMock.Object,
            _authorizationServiceMock.Object,
            _lockServiceMock.Object,
            _cartRepositoryMock.Object);
    }

    private static Mock<IResolveFieldContext<object>> CreateContextMock(bool authenticated)
    {
        var identity = authenticated
            ? new ClaimsIdentity("test")
            : new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);
        var userContext = new GraphQLUserContext(principal);

        var mock = new Mock<IResolveFieldContext<object>>();
        mock.Setup(x => x.UserContext).Returns(userContext);

        return mock;
    }

    private static CartAggregate CreateCartAggregate(string cartId = "cart-1")
    {
        var mock = new Mock<CartAggregate>(
            MockBehavior.Loose, null, null, null, null, null, null, null, null, null, null, null);

        var cartProperty = typeof(CartAggregate).GetProperty(nameof(CartAggregate.Cart));
        cartProperty!.SetValue(mock.Object, new ShoppingCart { Id = cartId });

        return mock.Object;
    }

    private void SetupLockServicePassthrough()
    {
        _lockServiceMock
            .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<Task<CartAggregate>>>()))
            .Returns<string, Func<Task<CartAggregate>>>((_, resolver) => resolver());
    }

    private void SetupAuthorizationSuccess()
    {
        _authorizationServiceMock
            .Setup(x => x.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object>(),
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());
    }

    [Fact]
    public async Task Resolve_AnonymousUser_ThrowsAuthorizationError()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.Command = new TestCartCommand { UserId = "user-1", CartId = "cart-1" };
        var context = CreateContextMock(authenticated: false);

        // Act
        var act = () => builder.TestResolve(context.Object);

        // Assert
        await act.Should().ThrowAsync<AuthorizationError>()
            .WithMessage("*Anonymous*");
    }

    [Fact]
    public async Task Resolve_AuthenticatedUser_ResolvesCartByCartId()
    {
        // Arrange
        var cartAggregate = CreateCartAggregate();
        _cartRepositoryMock
            .Setup(x => x.GetCartByIdAsync("cart-1", It.IsAny<string>()))
            .ReturnsAsync(cartAggregate);
        SetupAuthorizationSuccess();
        SetupLockServicePassthrough();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<TestCartCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cartAggregate);

        var builder = CreateBuilder();
        builder.Command = new TestCartCommand { UserId = "user-1", CartId = "cart-1" };
        var context = CreateContextMock(authenticated: true);

        // Act
        var (_, response) = await builder.TestResolve(context.Object);

        // Assert
        response.Should().BeSameAs(cartAggregate);
        _cartRepositoryMock.Verify(x => x.GetCartByIdAsync("cart-1", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Resolve_NoCartId_ResolvesCartByParams()
    {
        // Arrange
        var cartAggregate = CreateCartAggregate();
        _cartRepositoryMock
            .Setup(x => x.GetCartAsync(It.IsAny<TestCartCommand>(), It.IsAny<string>()))
            .ReturnsAsync(cartAggregate);
        SetupAuthorizationSuccess();
        SetupLockServicePassthrough();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<TestCartCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cartAggregate);

        var builder = CreateBuilder();
        builder.Command = new TestCartCommand { UserId = "user-1", StoreId = "store-1" };
        var context = CreateContextMock(authenticated: true);

        // Act
        await builder.TestResolve(context.Object);

        // Assert
        _cartRepositoryMock.Verify(x => x.GetCartAsync(It.IsAny<TestCartCommand>(), It.IsAny<string>()), Times.Once);
        _cartRepositoryMock.Verify(x => x.GetCartByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Resolve_CartFound_AuthorizesWithCanAccessCartRequirement()
    {
        // Arrange
        var cartAggregate = CreateCartAggregate();
        _cartRepositoryMock
            .Setup(x => x.GetCartByIdAsync("cart-1", It.IsAny<string>()))
            .ReturnsAsync(cartAggregate);
        SetupAuthorizationSuccess();
        SetupLockServicePassthrough();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<TestCartCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cartAggregate);

        var builder = CreateBuilder();
        builder.Command = new TestCartCommand { UserId = "user-1", CartId = "cart-1" };
        var context = CreateContextMock(authenticated: true);

        // Act
        await builder.TestResolve(context.Object);

        // Assert
        _authorizationServiceMock.Verify(x => x.AuthorizeAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<object>(o => o is ShoppingCart),
            It.Is<IEnumerable<IAuthorizationRequirement>>(r => r != null)),
            Times.Once);
    }

    [Fact]
    public async Task Resolve_CartNotFound_SkipsAuthorization()
    {
        // Arrange
        _cartRepositoryMock
            .Setup(x => x.GetCartByIdAsync("cart-1", It.IsAny<string>()))
            .ReturnsAsync((CartAggregate)null);
        SetupLockServicePassthrough();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<TestCartCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCartAggregate());

        var builder = CreateBuilder();
        builder.Command = new TestCartCommand { UserId = "user-1", CartId = "cart-1" };
        var context = CreateContextMock(authenticated: true);

        // Act
        await builder.TestResolve(context.Object);

        // Assert
        _authorizationServiceMock.Verify(x => x.AuthorizeAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<object>(),
            It.IsAny<IEnumerable<IAuthorizationRequirement>>()),
            Times.Never);
    }

    [Fact]
    public async Task Resolve_WithUserId_LocksOnUserId()
    {
        // Arrange
        var cartAggregate = CreateCartAggregate();
        _cartRepositoryMock
            .Setup(x => x.GetCartByIdAsync("cart-1", It.IsAny<string>()))
            .ReturnsAsync(cartAggregate);
        SetupAuthorizationSuccess();
        SetupLockServicePassthrough();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<TestCartCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cartAggregate);

        var builder = CreateBuilder();
        builder.Command = new TestCartCommand { UserId = "user-1", CartId = "cart-1" };
        var context = CreateContextMock(authenticated: true);

        // Act
        await builder.TestResolve(context.Object);

        // Assert
        _lockServiceMock.Verify(x => x.ExecuteAsync(
            "Cart:user-1",
            It.IsAny<Func<Task<CartAggregate>>>()),
            Times.Once);
    }

    [Fact]
    public async Task Resolve_AuthorizationFails_ThrowsForbidden()
    {
        // Arrange
        var cartAggregate = CreateCartAggregate();
        _cartRepositoryMock
            .Setup(x => x.GetCartByIdAsync("cart-1", It.IsAny<string>()))
            .ReturnsAsync(cartAggregate);
        _authorizationServiceMock
            .Setup(x => x.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object>(),
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Failed());

        var builder = CreateBuilder();
        builder.Command = new TestCartCommand { UserId = "user-1", CartId = "cart-1" };
        var context = CreateContextMock(authenticated: true);

        // Act
        var act = () => builder.TestResolve(context.Object);

        // Assert
        await act.Should().ThrowAsync<AuthorizationError>()
            .WithMessage("*Access denied*");
    }

    [Fact]
    public async Task Resolve_CallsAfterMediatorSendWithResponse()
    {
        // Arrange
        var cartAggregate = CreateCartAggregate("cart-42");
        _cartRepositoryMock
            .Setup(x => x.GetCartByIdAsync("cart-42", It.IsAny<string>()))
            .ReturnsAsync(cartAggregate);
        SetupAuthorizationSuccess();
        SetupLockServicePassthrough();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<TestCartCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cartAggregate);

        var builder = CreateBuilder();
        builder.Command = new TestCartCommand { UserId = "user-1", CartId = "cart-42" };
        var contextMock = CreateContextMock(authenticated: true);

        // Act
        await builder.TestResolve(contextMock.Object);

        // Assert
        builder.AfterMediatorSendResponse.Should().BeSameAs(cartAggregate);
    }

    #region Test helpers

    public class TestCartCommand : CartCommand
    {
    }

    private class TestableCartCommandBuilder(
        IMediator mediator,
        IAuthorizationService authorizationService,
        IDistributedLockService distributedLockService,
        ICartAggregateRepository cartRepository)
        : CartCommandBuilder<TestCartCommand, InputObjectGraphType>(
            mediator, authorizationService, distributedLockService, cartRepository)
    {
        public TestCartCommand Command { get; set; } = new();
        public CartAggregate AfterMediatorSendResponse { get; private set; }

        protected override string Name => "testCommand";

        // Override to avoid GraphQL argument deserialization.
        // CartCommandBuilder.GetRequest calls base.GetRequest (GraphQL arg binding) then sets UserId/OrgId.
        // We return a pre-configured command to isolate CartCommandBuilder behavior.
        protected override TestCartCommand GetRequest(IResolveFieldContext<object> context)
        {
            return Command;
        }

        // Override to avoid SetExpandedObjectGraph's deep reflection walk on CartAggregate mock,
        // which crashes due to null injected services. We record the call instead.
        protected override Task AfterMediatorSend(
            IResolveFieldContext<object> context, TestCartCommand request, CartAggregate response)
        {
            AfterMediatorSendResponse = response;
            return Task.CompletedTask;
        }

        public Task<(TestCartCommand, CartAggregate)> TestResolve(IResolveFieldContext<object> context)
        {
            return Resolve(context);
        }
    }

    #endregion
}
