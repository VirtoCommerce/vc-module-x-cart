using System;
using System.Collections.Generic;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Platform.Core.Settings;
using CartType = VirtoCommerce.CartModule.Core.ModuleConstants.CartType;//Moved to Core

namespace VirtoCommerce.XCart.Core
{
    public static class ModuleConstants
    {
        public static class SchemaConstants
        {
            public const string CommandName = "command";
        }

        [Obsolete("Use CartTypes.Wishlist instead", DiagnosticId = "VC0011", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
        public const string ListTypeName = CartType.Wishlist;

        [Obsolete("Use VirtoCommerce.CartModule.Core.Services.CartSharingScope instead", false, DiagnosticId = "VC0011", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions/")]
        public const string PrivateScope = CartSharingScope.Private;

        [Obsolete("Use VirtoCommerce.CartModule.Core.Services.CartSharingScope instead", false, DiagnosticId = "VC0011", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions/")]
        public const string OrganizationScope = CartSharingScope.Organization;

        public const int LineItemQualityLimit = 999999;

        public static class ValidationRuleSets
        {
            public const string Default = "default";
            public const string Strict = "strict";
            public const string Items = "items";
            public const string Shipments = "shipments";
            public const string Payments = "payments";
            public const string OrderCreate = "orderCreate";
            public const string All = "*";
        }

        /// <summary>
        /// Relative ordering of <see cref="Validators.ICartValidator{TContext}"/> links in the chain.
        /// Built-in validators use <see cref="Core"/> so they always run before module-supplied extensions.
        /// </summary>
        public static class ValidationOrder
        {
            public const int Core = -1000;
        }


        public static class Settings
        {
            public static class General
            {
                public static SettingDescriptor IsSelectedForCheckout { get; } = new SettingDescriptor
                {
                    Name = "XPurchase.IsSelectedForCheckout",
                    ValueType = SettingValueType.Boolean,
                    GroupName = "Cart|General",
                    DefaultValue = true,
                    IsPublic = true,
                };

                public static IEnumerable<SettingDescriptor> AllSettings
                {
                    get
                    {
                        yield return IsSelectedForCheckout;
                    }
                }
            }

            public static IEnumerable<SettingDescriptor> StoreLevelSettings
            {
                get
                {
                    yield return General.IsSelectedForCheckout;
                }
            }
        }
    }
}
