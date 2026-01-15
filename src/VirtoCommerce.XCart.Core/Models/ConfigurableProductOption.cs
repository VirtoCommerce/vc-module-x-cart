namespace VirtoCommerce.XCart.Core.Models;

public class ConfigurableProductOption
{
    public string ProductId { get; set; }

    public int Quantity { get; set; } = 1;

    public bool SelectedForCheckout { get; set; } = true;
}
