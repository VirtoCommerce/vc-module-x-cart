using System.Collections.Generic;

namespace VirtoCommerce.XCart.Core.Models;

public class InitializeCartPaymentResult
{
    public bool IsSuccess { get; set; }

    public string ErrorMessage { get; set; }

    public string StoreId { get; set; }

    public string PaymentId { get; set; }

    public string PaymentMethodCode { get; set; }

    public string PaymentActionType { get; set; }

    public string ActionRedirectUrl { get; set; }

    public string ActionHtmlForm { get; set; }

    public Dictionary<string, string> PublicParameters { get; set; } = [];
}
