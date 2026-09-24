using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands.BaseCommands;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Commands
{
    public class ScopedWishlistCommandHandlerBaseTests
    {
        [Fact]
        public void CreateScopeContext_CopiesTheCommandAndTheCaller()
        {
            var command = Command();
            command.AddSharedWithIds = ["org-1"];
            command.RemoveSharedWithIds = ["org-2"];
            command.Message = "Hello";

            var context = new TestHandler().Context(command);

            context.Scope.Should().Be("Customer");
            context.SharingKey.Should().Be("key-1");
            context.AddSharedWithIds.Should().Equal("org-1");
            context.RemoveSharedWithIds.Should().Equal("org-2");
            context.Message.Should().Be("Hello");
            context.CurrentUserId.Should().Be("user-1");
            context.CustomerName.Should().Be("Owner");
            context.CurrentOrganizationId.Should().Be("owner-org");
        }

        [Fact]
        public void CreateScopeContext_LegacySharedWithId_TravelsAsItself()
        {
            // Not merged into the adds here: what a single id means depends on the set the list already carries,
            // which only ICartSharingService can see (CartSharingService.ApplyLegacySharedWithId).
            var command = Command();
            command.SharedWithId = "org-legacy";
            command.AddSharedWithIds = ["org-1"];

            var context = new TestHandler().Context(command);

            context.LegacySharedWithId.Should().Be("org-legacy");
            context.AddSharedWithIds.Should().Equal("org-1");
        }

        [Fact]
        public void CreateScopeContext_NoTargets_LeavesTheListsNull()
        {
            var context = new TestHandler().Context(Command());

            context.AddSharedWithIds.Should().BeNull();
            context.RemoveSharedWithIds.Should().BeNull();
            context.Message.Should().BeNull();
        }

        private static ChangeWishlistCommand Command()
        {
            return new ChangeWishlistCommand
            {
                Scope = "Customer",
                SharingKey = "key-1",
                WishlistUserContext = new WishlistUserContext
                {
                    CurrentUserId = "user-1",
                    CurrentOrganizationId = "owner-org",
                    CurrentContact = new Contact { Name = "Owner" },
                },
            };
        }

        private sealed class TestHandler() : ScopedWishlistCommandHandlerBase<ChangeWishlistCommand>(Mock.Of<ICartAggregateRepository>(), Mock.Of<ICartSharingService>())
        {
            public WishlistScopeContext Context(ChangeWishlistCommand command) => CreateScopeContext(command);

            public override Task<CartAggregate> Handle(ChangeWishlistCommand request, CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }
        }
    }
}
