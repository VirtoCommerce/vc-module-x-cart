using System.Collections.Generic;
using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.XCart.Core.Commands;

public class CreateConfiguredLineItemCommand : ICommand<ExpConfigurationLineItem>
{
    public string StoreId { get; set; }

    public string UserId { get; set; }

    public string OrganizationId { get; set; }

    public string CurrencyCode { get; set; }

    public string CultureName { get; set; }

    public string ConfigurableProductId { get; set; }

    public IList<ProductConfigurationSection> ConfigurationSections { get; set; } = [];
}

public class ProductConfigurationSection
{
    public string SectionId { get; set; }

    public ConfigurableProductOption Value { get; set; }
}

public class ConfigurableProductOption
{
    public string ProductId { get; set; }

    public int Quantity { get; set; } = 1;
}

public class InputCreateConfiguredLineItemCommand : InputObjectGraphType<CreateConfiguredLineItemCommand>
{
    public InputCreateConfiguredLineItemCommand()
    {
        Field<StringGraphType>("storeId");
        Field<StringGraphType>("currencyCode");
        Field<StringGraphType>("cultureName");
        Field<NonNullGraphType<StringGraphType>>("configurableProductId");
        Field<ListGraphType<ConfigurationSectionInput>>("configurationSections");
    }
}

public class ConfigurationSectionInput : InputObjectGraphType<ProductConfigurationSection>
{
    public ConfigurationSectionInput()
    {
        Field<NonNullGraphType<StringGraphType>>("sectionId");
        Field<ConfigurableProductOptionInput>("value");
    }
}

public class ConfigurableProductOptionInput : InputObjectGraphType<ConfigurableProductOption>
{
    public ConfigurableProductOptionInput()
    {
        Field<NonNullGraphType<StringGraphType>>("productId");
        Field<NonNullGraphType<IntGraphType>>("quantity");
    }
}
