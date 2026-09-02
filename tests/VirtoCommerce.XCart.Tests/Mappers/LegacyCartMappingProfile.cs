using AutoMapper;
using CartAddress = VirtoCommerce.CartModule.Core.Model.Address;
using TaxAddress = VirtoCommerce.TaxModule.Core.Model.Address;

namespace VirtoCommerce.XCart.Tests.Mappers;

public class LegacyCartMappingProfile : Profile
{
    public LegacyCartMappingProfile()
    {
        CreateMap<CartAddress, TaxAddress>().IncludeAllDerived();
    }
}
