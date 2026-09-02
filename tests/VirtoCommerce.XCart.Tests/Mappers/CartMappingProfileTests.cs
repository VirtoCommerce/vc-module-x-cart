using AutoMapper;
using FluentAssertions;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Data.Services;
using Xunit;
using CartAddress = VirtoCommerce.CartModule.Core.Model.Address;
using TaxAddress = VirtoCommerce.TaxModule.Core.Model.Address;

namespace VirtoCommerce.XCart.Tests.Mappers;

[Collection(TaxAddressFactoryStateCollection.Name)]
public class CartMappingProfileTests
{
    private static readonly IMapper _legacyMapper = new MapperConfiguration(cfg => cfg.AddProfile(new LegacyCartMappingProfile())).CreateMapper();

    private readonly XCartMapper _mapper = new(new CartItemBuilder());

    [Fact]
    public void ToTaxAddress_DerivedTaxAddressRegistered_DerivedMapperOverridePopulatesExtraField()
    {
        // AutoMapper's polymorphic dispatch for a derived Address is replaced by AbstractTypeFactory
        // plus a derived mapper override that populates the field the derived type adds.
        AbstractTypeFactory<TaxAddress>.RegisterType<DerivedTaxAddress>();
        try
        {
            var mapper = new ExtraFieldPopulatingXCartMapper();
            var cartAddress = new CartAddress { Name = "name-1" };

            var result = mapper.ToTaxAddress(cartAddress);

            result.Should().BeOfType<DerivedTaxAddress>();
            result.Name.Should().Be("name-1");
            ((DerivedTaxAddress)result).ExtraField.Should().Be("populated-by-derived-mapper");
        }
        finally
        {
            AbstractTypeFactory<TaxAddress>.RemoveType<DerivedTaxAddress>();
        }
    }

    private class DerivedTaxAddress : TaxAddress
    {
        public string ExtraField { get; set; }
    }

    private class ExtraFieldPopulatingXCartMapper() : XCartMapper(new CartItemBuilder())
    {
        public override TaxAddress ToTaxAddress(CartAddress source)
        {
            var result = base.ToTaxAddress(source);
            ((DerivedTaxAddress)result).ExtraField = "populated-by-derived-mapper";
            return result;
        }
    }

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

// Every class registering or reading AbstractTypeFactory<TaxAddress> (process-global) joins this
// collection, so xUnit never runs them concurrently - see ToTaxAddress_DerivedTaxAddressRegistered...
[CollectionDefinition(Name)]
public class TaxAddressFactoryStateCollection
{
    public const string Name = "AbstractTypeFactory<TaxAddress> state";
}
