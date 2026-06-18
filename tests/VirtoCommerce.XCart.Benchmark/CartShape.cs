namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// The product-type mix a cart operation is exercised against. This is the real upstream
/// divergence: flat-SKU items take the <c>AddItemAsync</c> branch and a simple totals walk,
/// configured items carry a <c>LineItem</c> + N <c>ConfigurationItem</c> set.
/// </summary>
public enum CartShape
{
    /// <summary>Plain line items — baseline shape.</summary>
    Flat,

    /// <summary>Single-level configured — one LineItem with a configuration-item set each.</summary>
    Configured,
}
