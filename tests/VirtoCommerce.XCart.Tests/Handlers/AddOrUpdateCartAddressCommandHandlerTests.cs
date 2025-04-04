using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Moq;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands;
using VirtoCommerce.XCart.Tests.Helpers;
using Xunit;
using AddressType = VirtoCommerce.CoreModule.Core.Common.AddressType;


namespace VirtoCommerce.XCart.Tests.Handlers
{
    public class AddOrUpdateCartAddressCommandHandlerTests : XCartMoqHelper
    {
        [Fact]
        public async Task Handle_RequestWithAddress_AllAddressFieldsAreMapped()
        {
            // Arrange
            var addressType = AddressType.Billing;
            var address = GetAddress(addressType);

            var cartAggregate = GetValidCartAggregate();
            cartAggregate.Cart.Addresses.Clear();

            var cartAggregateRepositoryMock = new Mock<ICartAggregateRepository>();
            cartAggregateRepositoryMock
                .Setup(x => x.GetCartByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(cartAggregate);

            var request = new AddOrUpdateCartAddressCommand()
            {
                Address = address,
                CartId = cartAggregate.Cart.Id,
            };
            var handler = new AddOrUpdateCartAddressCommandHandler(cartAggregateRepositoryMock.Object);

            // Act
            var aggregate = await handler.Handle(request, CancellationToken.None);

            // Assert
            cartAggregate.Cart.Addresses.Should().ContainSingle(x => x.AddressType == addressType);
            cartAggregate.Cart.Addresses.Should().ContainSingle(x => x.Key == address.Key.Value);
            cartAggregate.Cart.Addresses.Should().ContainSingle(x => x.City == address.City.Value);
            cartAggregate.Cart.Addresses.Should().ContainSingle(x => x.CountryCode == address.CountryCode.Value);
            cartAggregate.Cart.Addresses.Should().ContainSingle(x => x.CountryName == address.CountryName.Value);
            cartAggregate.Cart.Addresses.Should().ContainSingle(x => x.Email == address.Email.Value);
            cartAggregate.Cart.Addresses.Should().ContainSingle(x => x.FirstName == address.FirstName.Value);
            cartAggregate.Cart.Addresses.Should().ContainSingle(x => x.LastName == address.LastName.Value);
            cartAggregate.Cart.Addresses.Should().ContainSingle(x => x.MiddleName == address.MiddleName.Value);
            cartAggregate.Cart.Addresses.Should().ContainSingle(x => x.Name == address.Name.Value);
            cartAggregate.Cart.Addresses.Should().ContainSingle(x => x.Line1 == address.Line1.Value);
            cartAggregate.Cart.Addresses.Should().ContainSingle(x => x.Line2 == address.Line2.Value);
            cartAggregate.Cart.Addresses.Should().ContainSingle(x => x.Organization == address.Organization.Value);
            cartAggregate.Cart.Addresses.Should().ContainSingle(x => x.Phone == address.Phone.Value);
            cartAggregate.Cart.Addresses.Should().ContainSingle(x => x.PostalCode == address.PostalCode.Value);
            cartAggregate.Cart.Addresses.Should().ContainSingle(x => x.RegionId == address.RegionId.Value);
            cartAggregate.Cart.Addresses.Should().ContainSingle(x => x.RegionName == address.RegionName.Value);
            cartAggregate.Cart.Addresses.Should().ContainSingle(x => x.Zip == address.Zip.Value);
            cartAggregate.Cart.Addresses.Should().ContainSingle(x => x.OuterId == address.OuterId.Value);
        }

        private ExpCartAddress GetAddress(AddressType addressType)
        {
            var address = _fixture.Create<ExpCartAddress>();
            address.AddressType = new Optional<int>((int)addressType);
            return address;
        }
    }
}
