using System.Collections.Generic;

namespace VirtoCommerce.XCart.Core.Models;

/// <summary>
/// A store payment method that can save cards, with the public parameters its card form needs (e.g. Accept.js keys).
/// </summary>
public class CardPaymentMethod
{
    public string PaymentMethodCode { get; set; }
    public string Name { get; set; }
    public string LogoUrl { get; set; }
    public IDictionary<string, string> PublicParameters { get; set; } = new Dictionary<string, string>();
}
