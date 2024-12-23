﻿using GraphQL.Types;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Schemas;

public class ConfigurationSectionInput : InputObjectGraphType<ProductConfigurationSection>
{
    public ConfigurationSectionInput()
    {
        Field<NonNullGraphType<StringGraphType>>("sectionId");
        Field<ConfigurableProductOptionInput>("value");
    }
}
