using System;
using GraphQL.DI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.FileExperienceApi.Core.Authorization;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.Platform.Core.Common;
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
using VirtoCommerce.XCart.Data.Validators;
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
            services.AddTransient<ICartSharingService, CartSharingService>();
            services.AddSingleton<ICartResponseGroupParser, CartResponseGroupParser>();
            services.AddTransient<CartAggregate>();
            services.AddTransient<Func<CartAggregate>>(provider => () => provider.CreateScope().ServiceProvider.GetRequiredService<CartAggregate>());
            services.AddTransient<IConfiguredLineItemContainerService, ConfiguredLineItemContainerService>();
            services.AddTransient<IConfigurationItemValidator, ConfigurationItemValidator>();
            services.AddSingleton<IFileAuthorizationRequirementFactory, ConfigurationItemFileAuthorizationRequirementFactory>();

            services.AddTransient<ICartValidatorRegistry, CartValidatorRegistry>();

            // Bridge each built-in validator through AbstractTypeFactory so a downstream
            // AbstractTypeFactory<T>.OverrideType still applies.
            services.AddTransient<ICartValidator<CartValidationContext>>(_ => AbstractTypeFactory<CartValidator>.TryCreateInstance());
            services.AddTransient<ICartValidator<PaymentValidationContext>>(_ => AbstractTypeFactory<CartPaymentValidator>.TryCreateInstance());
            services.AddTransient<ICartValidator<ShipmentValidationContext>>(_ => AbstractTypeFactory<CartShipmentValidator>.TryCreateInstance());
            services.AddTransient<ICartValidator<NewCartItem>>(_ => AbstractTypeFactory<NewCartItemValidator>.TryCreateInstance());
            services.AddTransient<ICartValidator<ItemQtyAdjustment>>(_ => AbstractTypeFactory<ItemQtyAdjustmentValidator>.TryCreateInstance());
            services.AddTransient<ICartValidator<PriceAdjustment>>(_ => AbstractTypeFactory<ChangeCartItemPriceValidator>.TryCreateInstance());

            // Pure DI: this wrapper has a constructor dependency the factory can't inject.
            services.AddTransient<ICartValidator<ConfigurationItemValidationContext>, ConfigurationItemContextValidator>();

            // No LineItemValidationContext link: the registry never dispatches it — CartValidator runs the nested
            // CartLineItemValidator (override via AbstractTypeFactory<CartLineItemValidator>).

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

        /// <summary>
        /// Replaces a single registration identified by both its service type and its concrete
        /// implementation type. Use it to swap one specific link in the CartValidator 
        /// </summary>
        public static IServiceCollection ReplaceImplementation<TService, TOldImplementation, TNewImplementation>(
            this IServiceCollection services)
            where TService : class
            where TOldImplementation : class, TService
            where TNewImplementation : class, TService
        {
            for (var i = 0; i < services.Count; i++)
            {
                var descriptor = services[i];
                if (descriptor.ServiceType == typeof(TService) && descriptor.ImplementationType == typeof(TOldImplementation))
                {
                    services[i] = ServiceDescriptor.Describe(typeof(TService), typeof(TNewImplementation), descriptor.Lifetime);
                    break;
                }
            }

            return services;
        }
    }
}
