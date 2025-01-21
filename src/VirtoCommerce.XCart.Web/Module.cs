using GraphQL.MicrosoftDI;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.StoreModule.Core.Model;
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
        appBuilder.UseScopedSchema<DataAssemblyMarker>("cart");

        var serviceProvider = appBuilder.ApplicationServices;

        // settings
        var settingsRegistrar = serviceProvider.GetRequiredService<ISettingsRegistrar>();
        settingsRegistrar.RegisterSettings(ModuleConstants.Settings.General.AllSettings, ModuleInfo.Id);
        settingsRegistrar.RegisterSettingsForType(ModuleConstants.Settings.StoreLevelSettings, nameof(Store));
    }

    public void Uninstall()
    {
        // Nothing to do here
    }
}
