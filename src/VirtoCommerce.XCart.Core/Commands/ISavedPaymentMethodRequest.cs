namespace VirtoCommerce.XCart.Core.Commands;

/// <summary>A saved-card query or mutation: it always acts for the signed-in user.</summary>
public interface ISavedPaymentMethodRequest
{
    string UserId { get; set; }
}
