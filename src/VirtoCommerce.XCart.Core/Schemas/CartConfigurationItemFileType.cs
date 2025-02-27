using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas;

public class CartConfigurationItemFileType : ExtendableGraphType<ConfigurationItemFile>
{
    public CartConfigurationItemFileType()
    {
        Field(x => x.Url, nullable: false).Description("Url of the file");
        Field(x => x.Name, nullable: false).Description("Name of the file");
        Field(x => x.Size, nullable: false).Description("Size of the file");
        Field(x => x.ContentType, nullable: true).Description("Mime type of the file");
    }
}
