using System.Collections.Generic;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Validators
{
    public class LineItemValidationContext
    {
        public LineItem LineItem { get; set; }
        public IEnumerable<CartProduct> AllCartProducts { get; set; }
    }
}
