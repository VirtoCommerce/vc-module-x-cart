using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace VirtoCommerce.XCart.Data.Extensions
{
    public static partial class FieldsExtensions
    {
        public static IList<string> ItemsToProductIncludeField(this IList<string> includeFields)
        {
            var result = includeFields?.Where(x => ProductFields().IsMatch(x))
                .Select(x => ProductFields().Replace(x, string.Empty)).ToList();

            return result;
        }

        [GeneratedRegex(@"^(items\.)?items\.(configurationItems\.)?product\.")]
        private static partial Regex ProductFields();
    }
}
