using System;
using GraphQL.DI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.FileExperienceApi.Core.Authorization;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.PricingModule.Core.Model;
using VirtoCommerce.TaxModule.Core.Model;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Pipelines;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Core.Validators;
using VirtoCommerce.XCart.Data.Authorization;
using VirtoCommerce.XCart.Data.Middlewares;
using VirtoCommerce.XCart.Data.Services;
using VirtoCommerce.XCatalog.Core.Models;

namespace VirtoCommerce.XCart.Data.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddXCart(this IServiceCollection services, IGraphQLBuilder graphQLBuilder)
        {
            services.AddSingleton<ScopedSchemaFactory<DataAssemblyMarker>>();

            services.AddSingleton<IAuthorizationHandler, CanAccessCartAuthorizationHandler>();
            services.AddTransient<ICartAggregateRepository, CartAggregateRepository>();
            services.AddTransient<ICartValidationContextFactory, CartValidationContextFactory>();
            services.AddTransient<ICartAvailMethodsService, CartAvailMethodsService>();
            services.AddTransient<ICartProductService, CartProductService>();
            services.AddTransient<ICartProductsLoaderService, CartProductService>();
            services.AddTransient<ISavedForLaterListService, SavedForLaterListService>();
            services.AddTransient<ICartSharingScopeCompatibilityService, CartSharingScopeCompatibilityService>();
            services.AddSingleton<ICartResponseGroupParser, CartResponseGroupParser>();
            services.AddTransient<CartAggregate>();
            services.AddTransient<Func<CartAggregate>>(provider => () => provider.CreateScope().ServiceProvider.GetRequiredService<CartAggregate>());
            services.AddTransient<IConfiguredLineItemContainerService, ConfiguredLineItemContainerService>();
            services.AddTransient<IConfigurationItemValidator, ConfigurationItemValidator>();
            services.AddSingleton<IFileAuthorizationRequirementFactory, ConfigurationItemFileAuthorizationRequirementFactory>();

            services.AddPipeline<SearchProductResponse>(builder =>
            {
                builder.AddMiddleware(typeof(EvalProductsWishlistsMiddleware));
            });

            services.AddPipeline<PromotionEvaluationContext>(builder =>
            {
                builder.AddMiddleware(typeof(LoadCartToEvalContextMiddleware));
            });

            services.AddPipeline<TaxEvaluationContext>(builder =>
            {
                builder.AddMiddleware(typeof(LoadCartToEvalContextMiddleware));
            });

            services.AddPipeline<PriceEvaluationContext>(builder =>
            {
                builder.AddMiddleware(typeof(LoadCartToEvalContextMiddleware));
            });

            services.AddPipeline<PromotionEvaluationContextCartMap>(builder =>
            {
                builder.AddMiddleware(typeof(MapPromoEvalContextMiddleware));
            });

            services.AddPipeline<ShipmentContextCartMap>();

            return services;
        }
    }
}
