using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.XCart.Data;
using VirtoCommerce.XCart.Data.Extensions;

namespace VirtoCommerce.XCart.Web;

public class Module : IModule, IHasConfiguration
{
    public ManifestModuleInfo ModuleInfo { get; set; }
    public IConfiguration Configuration { get; set; }

    public void Initialize(IServiceCollection serviceCollection)
    {
        var graphQlBuilder = new CustomGraphQLBuilder(serviceCollection);
        serviceCollection.AddXCart(graphQlBuilder);
    }

    public void PostInitialize(IApplicationBuilder appBuilder)
    {
        var playgroundOptions = appBuilder.ApplicationServices.GetService<IOptions<GraphQLPlaygroundOptions>>();
        appBuilder.UseSchemaGraphQL<ScopedSchemaFactory<DataAssemblyMarker>>(playgroundOptions?.Value?.Enable ?? true, "cart");
    }

    public void Uninstall()
    {
        // Nothing to do here
    }
}
