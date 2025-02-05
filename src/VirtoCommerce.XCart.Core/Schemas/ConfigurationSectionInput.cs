using GraphQL.Types;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Schemas;

public class ConfigurationSectionInput : InputObjectGraphType<ProductConfigurationSection>
{
    public ConfigurationSectionInput()
    {
        Field<NonNullGraphType<StringGraphType>>("sectionId");
        Field<StringGraphType>("customText");
        Field<CartConfigurationSectionTypeType>("type");
        Field<ConfigurableProductOptionInput>("value");
        //filed.Directives.Add(new DeprecatedDirective("Use Option property instead"));
        //filed.Directive = new DeprecatedDirective("Use Option property instead");
        Field<ConfigurableProductOptionInput>("option");
    }

    // public override void Initialize(ISchema schema)
    // {
    //     base.Initialize(schema);
    //
    //     var direcive = new DeprecatedDirective
    //     {
    //         Description = "Use Option property instead of Value",
    //         Arguments = new QueryArguments(new QueryArgument<StringGraphType>
    //         {
    //             Name = "value",
    //             Description = "Use Option property instead",
    //             DefaultValue = "No longer supported"
    //         })
    //     };
    //     schema.Directives.Register(direcive);
    // }
}
