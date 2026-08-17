using FluentAssertions;
using VirtoCommerce.XCart.Data.Services;
using Xunit;
using CartAddress = VirtoCommerce.CartModule.Core.Model.Address;

namespace VirtoCommerce.XCart.Tests.Mappers;

public class CartMappingProfileTests
{
    private readonly XCartMapper _mapper = new();

    [Fact]
    public void ToTaxAddress_CopiesAllFields()
    {
        // Arrange
        var cartAddress = new CartAddress()
        {
            Name = nameof(CartAddress),
        };

        // Act
        var taxAddress = _mapper.ToTaxAddress(cartAddress);

        // Assert
        taxAddress.Name.Should().Be(nameof(CartAddress));
    }

    [Fact]
    public void ToTaxAddress_NullSource_ReturnsNull()
    {
        _mapper.ToTaxAddress(null).Should().BeNull();
    }
}
