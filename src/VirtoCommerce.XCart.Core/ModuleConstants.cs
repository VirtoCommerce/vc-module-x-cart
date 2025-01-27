using System.Collections.Generic;
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
        public const string PrivateScope = "Private";
        public const string OrganizationScope = "Organization";

        public const int LineItemQualityLimit = 999999;

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
