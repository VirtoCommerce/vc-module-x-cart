using System;
using System.Linq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Extensions;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.TaxModule.Core.Model;
using VirtoCommerce.XCart.Core;
using TaxAddress = VirtoCommerce.TaxModule.Core.Model.Address;

namespace VirtoCommerce.XCart.Data.Services;

public static class CartAggregateMappingExtensions
{
    public static void MapTo(this CartAggregate source, PromotionEvaluationContext target)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.CartPromoEntries = [];

        // Tax and Promotion are computed only on primary-currency lines.
        foreach (var lineItem in source.CartCurrencySelectedLineItems)
        {
            var promoEntry = XCartMapper.BuildProductPromoEntry(lineItem);
            var cartProduct = source.CartProducts[source.GetCartProductKey(lineItem)];
            if (cartProduct != null)
            {
                promoEntry.InStockQuantity = (int)(cartProduct.Inventory?.InStockQuantity ?? 0);
                promoEntry.Outline = cartProduct.Product.Outlines?.GetOutlinePath(cartProduct.Product.CatalogId);
                promoEntry.ParentId = cartProduct.Product.MainProductId;
            }

            target.CartPromoEntries.Add(promoEntry);
        }

        target.CartTotal = source.Cart.SubTotal;
        target.StoreId = source.Cart.StoreId;
        target.Coupons = source.Cart.Coupons?.ToList();
        target.Currency = source.Cart.Currency;
        target.CustomerId = target.UserId = source.Cart.CustomerId;
        target.ContactId = source.Member?.Id;
        target.OrganizationId = source.Cart.OrganizationId;
        target.UserGroups = source.Member?.Groups.ToArray();
        target.IsRegisteredUser = !source.Cart.IsAnonymous;
        target.Language = source.Cart.LanguageCode;
        // Cart line items are the default promo items.
        target.PromoEntries = target.CartPromoEntries;

        if (!source.Cart.Shipments.IsNullOrEmpty())
        {
            var shipment = source.Cart.Shipments.First();
            target.ShipmentMethodCode = shipment.ShipmentMethodCode;
            target.ShipmentMethodOption = shipment.ShipmentMethodOption;
            target.ShipmentMethodPrice = shipment.Price;
        }

        if (!source.Cart.Payments.IsNullOrEmpty())
        {
            var payment = source.Cart.Payments.First();
            target.PaymentMethodCode = payment.PaymentGatewayCode;
            target.PaymentMethodPrice = payment.Price;
        }

        target.IsEveryone = true;
    }

    public static void MapTo(this CartAggregate source, TaxEvaluationContext target)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.StoreId = source.Cart.StoreId;
        target.Code = source.Cart.Name;
        target.Type = "Cart";
        target.CustomerId = source.Cart.CustomerId;
        target.Currency = source.Cart.Currency;

        // Tax and Promotion are computed only on primary-currency lines.
        foreach (var lineItem in source.CartCurrencySelectedLineItems)
        {
            var taxLine = AbstractTypeFactory<TaxLine>.TryCreateInstance();
            taxLine.Id = lineItem.Id;
            taxLine.Code = lineItem.Sku;
            taxLine.Name = lineItem.Name;
            // Special case when a product has a 100% discount and the tax still needs to be calculated on the old value.
            taxLine.TaxType = lineItem.TaxType;
            taxLine.Amount = lineItem.ExtendedPrice > 0 ? lineItem.ExtendedPrice : lineItem.SalePrice;
            taxLine.Quantity = lineItem.Quantity;
            taxLine.Price = lineItem.PlacedPrice;
            taxLine.TypeName = "item";
            target.Lines.Add(taxLine);
        }

        foreach (var shipment in source.Cart.Shipments ?? Array.Empty<Shipment>())
        {
            var totalTaxLine = AbstractTypeFactory<TaxLine>.TryCreateInstance();
            totalTaxLine.Id = shipment.Id;
            totalTaxLine.Code = shipment.ShipmentMethodCode;
            totalTaxLine.Name = shipment.ShipmentMethodOption;
            totalTaxLine.TaxType = shipment.TaxType;
            totalTaxLine.Amount = shipment.Total > 0 ? shipment.Total : shipment.Price;
            totalTaxLine.TypeName = "shipment";
            target.Lines.Add(totalTaxLine);

            if (shipment.DeliveryAddress != null)
            {
                var taxAddress = AbstractTypeFactory<TaxAddress>.TryCreateInstance();
                XCartMapper.CopyAddressFields(shipment.DeliveryAddress, taxAddress);
                target.Address = taxAddress;
            }
        }

        foreach (var payment in source.Cart.Payments ?? Array.Empty<Payment>())
        {
            var totalTaxLine = AbstractTypeFactory<TaxLine>.TryCreateInstance();
            totalTaxLine.Id = payment.Id;
            totalTaxLine.Code = payment.PaymentGatewayCode;
            totalTaxLine.Name = payment.PaymentGatewayCode;
            totalTaxLine.TaxType = payment.TaxType;
            totalTaxLine.Amount = payment.Total > 0 ? payment.Total : payment.Price;
            totalTaxLine.TypeName = "payment";
            target.Lines.Add(totalTaxLine);
        }
    }
}
