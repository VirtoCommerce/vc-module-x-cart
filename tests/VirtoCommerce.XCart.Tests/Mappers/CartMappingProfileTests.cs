using AutoMapper;
using FluentAssertions;
using VirtoCommerce.XCart.Data.Mapping;
using Xunit;
using CartAddress = VirtoCommerce.CartModule.Core.Model.Address;
using TaxAddress = VirtoCommerce.TaxModule.Core.Model.Address;

namespace VirtoCommerce.XCart.Tests.Mappers;

public class CartMappingProfileTests
{
    private readonly IMapper _mapper;

    public CartMappingProfileTests()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<CartMappingProfile>();
            cfg.AddProfile<CartTestDerivedMappingProfile>();
        });

        _mapper = configuration.CreateMapper();
    }

    [Fact]
    public void MappingProfile_Should_ConvertAddresses()
    {
        // Arrange
        var cartAddress = new CartAddress()
        {
            Name = nameof(CartAddress),
        };

        var taxAddress = new TaxAddress();

        // Act
        _mapper.Map(cartAddress, taxAddress);

        // Assert
        taxAddress.Name.Should().Be(nameof(CartAddress));
    }

    [Fact]
    public void MappingProfile_Should_ConvertExtendedAddresses()
    {
        // Arrange
        var cartAddress = new CartAddress2()
        {
            Name = nameof(CartAddress),
            Extension = nameof(CartAddress2),
        };

        var taxAddress = new TaxAddress2();

        // Act
        _mapper.Map((CartAddress)cartAddress, taxAddress);

        // Assert
        taxAddress.Extension.Should().Be(nameof(CartAddress2));
    }
}

public class CartAddress2 : CartAddress
{
    public string Extension { get; set; }
}

public class TaxAddress2 : TaxAddress
{
    public string Extension { get; set; }
}

public class CartTestDerivedMappingProfile : Profile
{
    public CartTestDerivedMappingProfile()
    {
        CreateMap<CartAddress2, TaxAddress2>();
    }
}
