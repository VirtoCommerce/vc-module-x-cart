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
        /// Default order for the built-in validators
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
