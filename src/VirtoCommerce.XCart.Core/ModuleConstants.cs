using System;
using System.Collections.Generic;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.XCart.Core
{
    public static class ModuleConstants
    {
        public static class SchemaConstants
        {
            public const string CommandName = "command";
        }

        public const string ListTypeName = "Wishlist";

        [Obsolete("Use VirtoCommerce.CartModule.Core.Services.CartSharingModes instead")]//TODO: Version info?
        public const string PrivateScope = CartSharingModes.Private;

        [Obsolete("Use VirtoCommerce.CartModule.Core.Services.CartSharingModes instead")]//TODO: Version info?
        public const string OrganizationScope = CartSharingModes.Organization;

        public const int LineItemQualityLimit = 999999;

        // We use this response group for all X-Cart related operations to avoid recalculation of totals and improve performance.
        public static string XCartResponseGroup { get; set; } = (CartResponseGroup.Full & ~CartResponseGroup.RecalculateTotals).ToString();

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
