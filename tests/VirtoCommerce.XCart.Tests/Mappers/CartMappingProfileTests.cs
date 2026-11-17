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
}

// Every class registering or reading AbstractTypeFactory<TaxAddress> (process-global) joins this
// collection, so xUnit never runs them concurrently - see ToTaxAddress_DerivedTaxAddressRegistered...
[CollectionDefinition(Name)]
public class TaxAddressFactoryStateCollection
{
    public const string Name = "AbstractTypeFactory<TaxAddress> state";
}
