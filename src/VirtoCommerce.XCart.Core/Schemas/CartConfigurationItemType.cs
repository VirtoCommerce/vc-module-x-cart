using GraphQL.Types;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class CartConfigurationItemType : ExtendableGraphType<ConfigurationItem>
    {
        public CartConfigurationItemType()
        {
            Field(x => x.Id, nullable: false).Description("Configuration item ID");
            Field(x => x.Name, nullable: true).Description("Configuration item name");
            Field(x => x.SectionId, nullable: false).Description("Configuration item section ID");
            Field(x => x.ProductId, nullable: true).Description("Configuration item product ID");
            Field(x => x.Quantity, nullable: true).Description("Configuration item product quantity");
            Field(x => x.CustomText, nullable: true).Description("Custom text for 'Text' configuration item section");
            Field(x => x.Type, nullable: false).Description("Configuration item type. Possible values: 'Product', 'Text', 'File'");

            ExtendableField<ListGraphType<CartConfigurationItemFileType>>(nameof(ConfigurationItem.Files),
                resolve: context => context.Source.Files,
                description: "List of files for 'File' configuration item section");
        }
    }
}
