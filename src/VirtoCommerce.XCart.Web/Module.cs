using GraphQL.MicrosoftDI;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Data;
using VirtoCommerce.XCart.Data.Extensions;

namespace VirtoCommerce.XCart.Web;

public class Module : IModule, IHasConfiguration
{
    public ManifestModuleInfo ModuleInfo { get; set; }
    public IConfiguration Configuration { get; set; }

    public void Initialize(IServiceCollection serviceCollection)
    {
        var graphQlBuilder = new GraphQLBuilder(serviceCollection, builder =>
        {
            builder.AddSchema(serviceCollection, typeof(CoreAssemblyMarker), typeof(DataAssemblyMarker));
        });

        serviceCollection.AddXCart(graphQlBuilder);
    }

    public void PostInitialize(IApplicationBuilder appBuilder)
    {
        // disable scoped schema
        //appBuilder.UseScopedSchema<DataAssemblyMarker>("cart");
    }

    public void Uninstall()
    {
        // Nothing to do here
    }
}
