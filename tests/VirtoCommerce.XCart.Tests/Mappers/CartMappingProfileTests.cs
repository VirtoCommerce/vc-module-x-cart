using AutoMapper;
using FluentAssertions;
using VirtoCommerce.XCart.Data.Services;
using Xunit;
using CartAddress = VirtoCommerce.CartModule.Core.Model.Address;
using TaxAddress = VirtoCommerce.TaxModule.Core.Model.Address;

namespace VirtoCommerce.XCart.Tests.Mappers;

public class CartMappingProfileTests
{
    private static readonly IMapper _legacyMapper = new MapperConfiguration(cfg => cfg.AddProfile(new LegacyCartMappingProfile())).CreateMapper();

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

    [Fact]
    public void ToTaxAddress_MatchesLegacyMapper()
    {
        var source = new CartAddress
        {
            AddressType = VirtoCommerce.CoreModule.Core.Common.AddressType.Shipping,
            Key = "key-1",
            Name = "name-1",
            Organization = "org-1",
            CountryCode = "US",
            CountryName = "United States",
            City = "Seattle",
            PostalCode = "98101",
            Zip = "98101",
            Line1 = "line1",
            Line2 = "line2",
            RegionId = "WA",
            RegionName = "Washington",
            FirstName = "John",
            MiddleName = "M",
            LastName = "Doe",
            Phone = "555-0100",
            Email = "john.doe@example.com",
            OuterId = "outer-1",
            IsDefault = true,
            Description = "description-1",
        };

        var legacy = _legacyMapper.Map<TaxAddress>(source);
        var actual = _mapper.ToTaxAddress(source);

        actual.Should().BeEquivalentTo(legacy);
    }
}
